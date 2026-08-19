// <copyright file="ReturnConfirmedToArHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.OrderManagement.Domain.Events;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="ReturnConfirmedEvent"/> (raised when a customer return / RMA is
/// confirmed) and generates a credit memo against the original customer, then posts it to
/// the General Ledger (credit AR control, debit revenue). This is the reverse leg of the
/// Sales -> AR integration: returns now produce the offsetting AR entry just as shipments
/// produced the original invoice. Completes the Purchase -> Inventory -> Sales -> Return loop.
/// </summary>
public sealed class ReturnConfirmedToArHandler : IDomainEventHandler<ReturnConfirmedEvent>
{
    private const string ArControlAccountNumber = "1200";
    private const string RevenueAccountNumber = "4000";

    private readonly ArDbContext _arContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IPeriodService _periodService;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;

    public ReturnConfirmedToArHandler(
        ArDbContext arContext,
        PlatformDbContext platformContext,
        IPeriodService periodService,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser)
    {
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task HandleAsync(ReturnConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0)
            return;

        var customer = await _arContext.Customers
            .FirstOrDefaultAsync(c => c.Id == domainEvent.CustomerId, cancellationToken);
        if (customer is null)
            return; // Customer not yet migrated into AR; nothing to credit.

        var period = await _periodService.GetCurrentPeriodAsync(domainEvent.CompanyId, cancellationToken);
        var memoDate = DateTimeOffset.UtcNow;

        var batch = new InvoiceBatch(
            domainEvent.CompanyId,
            $"RMA-{domainEvent.ReturnNumber}",
            $"Credit memo for return {domainEvent.ReturnNumber}",
            memoDate,
            period?.Id ?? Guid.Empty);

        var memo = batch.AddCreditDebitMemo(
            domainEvent.CustomerId,
            $"CM-{domainEvent.ReturnNumber}",
            memoDate,
            domainEvent.ShipmentId,
            $"Customer return {domainEvent.ReturnNumber}");

        memo.SetMemoType(CreditDebitMemoType.CreditMemo);

        foreach (var line in domainEvent.Lines)
        {
            var discountAmount = (line.Quantity * line.UnitPrice) * (line.DiscountPercent / 100m);
            var taxAmount = ((line.Quantity * line.UnitPrice) - discountAmount) * (line.TaxPercent / 100m);

            memo.AddLine(
                line.AccountId ?? Guid.Empty,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                taxAmount,
                discountAmount);
        }

        _arContext.InvoiceBatches.Add(batch);
        _arContext.CreditDebitMemos.Add(memo);
        memo.Apply();
        await _arContext.SaveChangesAsync(cancellationToken);

        // Post the credit memo to the General Ledger: credit AR control, debit revenue.
        var arControl = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == domainEvent.CompanyId && a.AccountNumber == ArControlAccountNumber, cancellationToken);

        if (arControl is not null)
        {
            var lines = new System.Collections.Generic.List<PostingLine>();

            foreach (var line in memo.Lines)
            {
                var revenueAccountId = line.AccountId != Guid.Empty
                    ? line.AccountId
                    : (await _platformContext.Accounts
                        .FirstOrDefaultAsync(a => a.CompanyId == domainEvent.CompanyId && a.AccountNumber == RevenueAccountNumber, cancellationToken))?.Id
                      ?? Guid.Empty;

                // Dr Revenue, Cr Accounts Receivable control (reverse of the invoice posting).
                lines.Add(new PostingLine
                {
                    AccountId = revenueAccountId,
                    Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
                    Debit = line.TotalAmount,
                    Credit = 0m,
                    Currency = "USD"
                });

                lines.Add(new PostingLine
                {
                    AccountId = arControl.Id,
                    Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
                    Debit = 0m,
                    Credit = line.TotalAmount,
                    Currency = "USD"
                });
            }

            if (lines.Count > 0)
            {
                var postedBy = _currentUser.UserId ?? "system";
                var postingEvent = CanonicalPostingEvent.Create(
                    "AR",
                    $"CM-{domainEvent.ReturnNumber}",
                    domainEvent.CompanyId,
                    period?.Id ?? Guid.Empty,
                    domainEvent.CompanyId.ToString(),
                    (period?.Id ?? Guid.Empty).ToString(),
                    memoDate,
                    lines,
                    PostingMetadata.Create(postedBy, Guid.NewGuid(), customerId: domainEvent.CustomerId.ToString(), projectId: null));

                await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
            }
        }
    }
}
