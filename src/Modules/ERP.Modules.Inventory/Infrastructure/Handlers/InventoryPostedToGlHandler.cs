// <copyright file="InventoryPostedToGlHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Domain.Entities;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.Inventory.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Inventory.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="InventoryTransactionPostedEvent"/> (raised when an
/// inventory transaction is committed) and creates a balanced General Ledger
/// journal through the canonical posting contract. This closes the
/// previously-missing Inventory -> GL integration: inventory receipts debit the
/// inventory asset account and credit goods-received (GRNI); issues debit COGS
/// and credit inventory; adjustments post to inventory with a variance offset.
/// Internal transfers do not change total inventory value and are skipped.
/// </summary>
public sealed class InventoryPostedToGlHandler : IDomainEventHandler<InventoryTransactionPostedEvent>
{
    // Default chart-of-accounts numbers; overridden per-item via ItemGLAccountDefaults.
    private const string InventoryAssetNumber = "1400";
    private const string CogsNumber = "5000";
    private const string GrniNumber = "2010";
    private const string VarianceNumber = "5900";
    private const string ScrapLossNumber = "6900";

    private readonly InventoryDbContext _inventoryContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;

    public InventoryPostedToGlHandler(
        InventoryDbContext inventoryContext,
        PlatformDbContext platformContext,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser)
    {
        _inventoryContext = inventoryContext ?? throw new ArgumentNullException(nameof(inventoryContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task HandleAsync(InventoryTransactionPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var transaction = await _inventoryContext.InventoryTransactions
            .FirstOrDefaultAsync(t => t.Id == domainEvent.TransactionId, cancellationToken);

        if (transaction is null)
            throw new InvalidOperationException($"Inventory transaction {domainEvent.TransactionId} not found while posting to GL.");

        var type = transaction.TransactionType;

        // Internal transfers do not change total inventory value; no GL entry.
        if (type is TransactionType.Transfer or TransactionType.TransferIn or TransactionType.TransferOut)
            return;

        var amount = Math.Abs(transaction.ExtendedCost);
        if (amount == 0m)
            return;

        var defaults = await _inventoryContext.ItemGLAccountDefaults
            .FirstOrDefaultAsync(d => d.ItemId == transaction.ItemId, cancellationToken);

        var inventoryAssetId = await ResolveAccountAsync(transaction.CompanyId, defaults?.InventoryAssetAccountId, InventoryAssetNumber, cancellationToken);
        var cogsId = await ResolveAccountAsync(transaction.CompanyId, defaults?.COGSAccountId, CogsNumber, cancellationToken);
        var grniId = await ResolveAccountAsync(transaction.CompanyId, null, GrniNumber, cancellationToken);
        var varianceId = await ResolveAccountAsync(transaction.CompanyId, defaults?.VarianceAccountId, VarianceNumber, cancellationToken);

        var lines = new List<PostingLine>();

        switch (type)
        {
            case TransactionType.Receipt:
                // Dr Inventory asset, Cr Goods Received Not Invoiced.
                lines.Add(MakeLine(inventoryAssetId, amount, 0m));
                lines.Add(MakeLine(grniId, 0m, amount));
                break;

            case TransactionType.Issue:
                // Dr COGS, Cr Inventory asset.
                lines.Add(MakeLine(cogsId, amount, 0m));
                lines.Add(MakeLine(inventoryAssetId, 0m, amount));
                break;

            case TransactionType.Adjustment:
                // Net movement to inventory, offset by variance.
                if (transaction.Quantity >= 0)
                {
                    lines.Add(MakeLine(inventoryAssetId, amount, 0m));
                    lines.Add(MakeLine(varianceId, 0m, amount));
                }
                else
                {
                    lines.Add(MakeLine(varianceId, amount, 0m));
                    lines.Add(MakeLine(inventoryAssetId, 0m, amount));
                }

                break;

            case TransactionType.Scrap:
                // Dr Scrap/obsolescence loss, Cr Inventory asset (write-off).
                var scrapLossId = await ResolveAccountAsync(transaction.CompanyId, null, ScrapLossNumber, cancellationToken);
                lines.Add(MakeLine(scrapLossId, amount, 0m));
                lines.Add(MakeLine(inventoryAssetId, 0m, amount));
                break;

            default:
                return;
        }

        var period = await ResolveFiscalPeriodAsync(transaction.CompanyId, transaction.TransactionDate, cancellationToken);
        var postedBy = _currentUser.UserId ?? "system";

        var postingEvent = CanonicalPostingEvent.Create(
            "INV",
            $"INV-TXN-{transaction.Id:N}",
            transaction.CompanyId,
            period?.Id ?? transaction.CompanyId,
            transaction.CompanyId.ToString(),
            (period?.Id ?? transaction.CompanyId).ToString(),
            transaction.TransactionDate,
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid(), customerId: null, projectId: transaction.ProjectId?.ToString()));

        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
    }

    private static PostingLine MakeLine(Guid accountId, decimal debit, decimal credit) => new PostingLine
    {
        AccountId = accountId,
        Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
        Debit = debit,
        Credit = credit,
        Currency = "USD"
    };

    private async Task<Guid> ResolveAccountAsync(
        Guid companyId, Guid? explicitAccountId, string fallbackNumber, CancellationToken cancellationToken)
    {
        if (explicitAccountId.HasValue)
            return explicitAccountId.Value;

        var account = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountNumber == fallbackNumber, cancellationToken);

        if (account is null)
        {
            throw new InvalidOperationException(
                $"GL account '{fallbackNumber}' for company {companyId} was not found. " +
                "Seed a chart-of-accounts entry for the inventory asset / COGS / GRNI / variance accounts before posting inventory transactions.");
        }

        return account.Id;
    }

    private async Task<FiscalPeriod?> ResolveFiscalPeriodAsync(
        Guid companyId, DateTime transactionDate, CancellationToken cancellationToken)
    {
        var date = new DateTimeOffset(transactionDate);
        return await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.StartDate <= date && p.EndDate >= date)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
