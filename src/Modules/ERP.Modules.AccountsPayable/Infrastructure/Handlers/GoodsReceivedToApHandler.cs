// <copyright file="GoodsReceivedToApHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
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

    public GoodsReceivedToApHandler(ApDbContext apContext, PurchasingDbContext purchasingContext)
    {
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _purchasingContext = purchasingContext ?? throw new ArgumentNullException(nameof(purchasingContext));
    }

    public async Task HandleAsync(GoodsReceivedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0)
            return;

        foreach (var line in domainEvent.Lines)
        {
            var orderedQuantity = await ResolveOrderedQuantityAsync(line.PurchaseOrderLineId, cancellationToken);
            var overReceipt = orderedQuantity.HasValue &&
                              line.QuantityReceived > orderedQuantity.Value * (1 + OverReceiptTolerance);

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

        if (_apContext.ChangeTracker.HasChanges())
            await _apContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal?> ResolveOrderedQuantityAsync(Guid? purchaseOrderLineId, CancellationToken cancellationToken)
    {
        if (purchaseOrderLineId is null)
            return null;

        var poLine = await _purchasingContext.PurchaseOrderLines
            .FirstOrDefaultAsync(l => l.Id == purchaseOrderLineId.Value, cancellationToken);

        return poLine?.Quantity;
    }
}
