// <copyright file="GoodsReceivedToApHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Domain.Events;
using ERP.Modules.Purchasing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="GoodsReceivedEvent"/> (raised when a purchasing goods
/// receipt is posted) and records the "received" leg of the Accounts Payable
/// 3-way match. Each receipt line is persisted as a <see cref="GoodsReceiptMatch"/>
/// row so the 3-way match (PO &lt;-&gt; Receipt &lt;-&gt; Invoice) can correlate the
/// received quantity against the eventual voucher's invoice quantity. When the
/// received quantity exceeds the ordered quantity by more than the standard
/// tolerance it is flagged for over-receipt approval (spec §6), closing the
/// Purchasing -&gt; AP goods-receipt integration that was previously unwired.
/// </summary>
public sealed class GoodsReceivedToApHandler : IDomainEventHandler<GoodsReceivedEvent>
{
    private const decimal OverReceiptTolerance = 0.05m;

    private readonly ApDbContext _apContext;
    private readonly PurchasingDbContext _purchasingContext;
    private readonly IPeriodService _periodService;

    public GoodsReceivedToApHandler(ApDbContext apContext, PurchasingDbContext purchasingContext, IPeriodService periodService)
    {
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _purchasingContext = purchasingContext ?? throw new ArgumentNullException(nameof(purchasingContext));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
    }

    public async Task HandleAsync(GoodsReceivedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0)
            return;

        var hasOpenAccrual = await _apContext.GrirAccruals
            .AnyAsync(a => a.ReceiptId == domainEvent.ReceiptId && a.ReversedByAccrualId == null, cancellationToken);
        if (hasOpenAccrual)
            return;

        var poLineIds = domainEvent.Lines
            .Where(l => l.PurchaseOrderLineId.HasValue)
            .Select(l => l.PurchaseOrderLineId!.Value)
            .Distinct()
            .ToList();

        var poLinesById = poLineIds.Count == 0
            ? new Dictionary<Guid, PurchaseOrderLine>()
            : await _purchasingContext.PurchaseOrderLines
                .Where(l => poLineIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, cancellationToken);

        foreach (var line in domainEvent.Lines)
        {
            var poLine = line.PurchaseOrderLineId.HasValue ? poLinesById.GetValueOrDefault(line.PurchaseOrderLineId.Value) : null;
            var overReceipt = poLine != null && line.QuantityReceived > poLine.Quantity * (1 + OverReceiptTolerance);

            _apContext.GoodsReceiptMatches.Add(new GoodsReceiptMatch(
                domainEvent.CompanyId,
                domainEvent.ReceiptId,
                domainEvent.ReceiptNumber,
                domainEvent.PurchaseOrderId,
                domainEvent.VendorId,
                line.PurchaseOrderLineId,
                line.ItemId,
                line.Description,
                line.QuantityReceived,
                line.UnitOfMeasure,
                domainEvent.ReceivedDate,
                overReceipt));
        }

        await TryAddGrirAccrualAsync(domainEvent, poLinesById, cancellationToken);

        if (_apContext.ChangeTracker.HasChanges())
            await _apContext.SaveChangesAsync(cancellationToken);
    }

    private async Task TryAddGrirAccrualAsync(GoodsReceivedEvent domainEvent, Dictionary<Guid, PurchaseOrderLine> poLinesById, CancellationToken cancellationToken)
    {
        if (!domainEvent.VendorId.HasValue)
            return;

        var accrualAmount = domainEvent.Lines
            .Where(l => l.PurchaseOrderLineId.HasValue && poLinesById.ContainsKey(l.PurchaseOrderLineId.Value))
            .Sum(l => Math.Round(l.QuantityReceived * poLinesById[l.PurchaseOrderLineId!.Value].UnitPrice, 2, MidpointRounding.AwayFromZero));

        if (accrualAmount <= 0)
            return;

        var period = await _periodService.GetCurrentPeriodAsync(domainEvent.CompanyId, cancellationToken);
        if (period == null)
            return;

        _apContext.GrirAccruals.Add(new GrirAccrual(
            domainEvent.CompanyId,
            domainEvent.VendorId.Value,
            domainEvent.PurchaseOrderId,
            domainEvent.ReceiptId,
            accrualAmount,
            period.Id));
    }
}
