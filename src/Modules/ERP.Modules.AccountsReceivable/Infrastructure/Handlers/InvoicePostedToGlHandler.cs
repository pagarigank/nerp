// <copyright file="InvoicePostedToGlHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="InvoiceBatchPostedEvent"/> (raised when an AR invoice
/// batch is posted) and creates a balanced General Ledger journal through the
/// canonical posting contract. This closes the previously-missing AR -> GL
/// integration: AR invoices now debit the Accounts Receivable control account
/// and credit the invoice revenue/distribution accounts, exactly mirroring the
/// AP -> GL path. Without this handler AR revenue and the AR control balance
/// were never reflected in the General Ledger.
/// </summary>
public sealed class InvoicePostedToGlHandler : IDomainEventHandler<InvoiceBatchPostedEvent>
{
    private const string ArControlAccountNumber = "1200";

    private readonly ArDbContext _arContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;

    public InvoicePostedToGlHandler(
        ArDbContext arContext,
        PlatformDbContext platformContext,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser)
    {
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task HandleAsync(InvoiceBatchPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // The event is dispatched from inside ArDbContext.SaveChangesAsync, before
        // the batch is committed. The handler's ArDbContext is the same scoped
        // instance that holds the uncommitted aggregate, so read it from the
        // change tracker (Local) first; otherwise fall back to the database.
        var batch = _arContext.InvoiceBatches.Local
            .FirstOrDefault(b => b.Id == domainEvent.BatchId);

        if (batch is null)
        {
            batch = await _arContext.InvoiceBatches
                .Include(b => b.Invoices)
                .ThenInclude(i => i.Lines)
                .FirstOrDefaultAsync(b => b.Id == domainEvent.BatchId, cancellationToken);
        }

        if (batch is null)
            throw new InvalidOperationException($"Invoice batch {domainEvent.BatchId} not found while posting to GL.");

        var arControl = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == domainEvent.CompanyId && a.AccountNumber == ArControlAccountNumber, cancellationToken);

        if (arControl is null)
        {
            throw new InvalidOperationException(
                $"AR control account '{ArControlAccountNumber}' for company {domainEvent.CompanyId} was not found. " +
                "Seed a chart-of-accounts Accounts Receivable control account (number 1200) before posting AR invoices.");
        }

        var lines = new System.Collections.Generic.List<PostingLine>();

        foreach (var invoice in batch.Invoices)
        {
            foreach (var line in invoice.Lines)
            {
                // Dr Accounts Receivable control, Cr revenue/distribution account.
                lines.Add(new PostingLine
                {
                    AccountId = arControl.Id,
                    Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
                    Debit = line.TotalAmount,
                    Credit = 0m,
                    Currency = "USD"
                });

                lines.Add(new PostingLine
                {
                    AccountId = line.AccountId,
                    Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
                    Debit = 0m,
                    Credit = line.TotalAmount,
                    Currency = "USD"
                });
            }
        }

        if (lines.Count == 0)
        {
            return;
        }

        var postedBy = _currentUser.UserId ?? "system";

        var postingEvent = CanonicalPostingEvent.Create(
            "AR",
            $"INV-{domainEvent.BatchNumber}",
            domainEvent.CompanyId,
            domainEvent.FiscalPeriodId,
            domainEvent.CompanyId.ToString(),
            domainEvent.FiscalPeriodId.ToString(),
            domainEvent.PostingDate,
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid(), customerId: null, projectId: null));

        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
    }
}