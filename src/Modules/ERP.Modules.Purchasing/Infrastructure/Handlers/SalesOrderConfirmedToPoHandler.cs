// <copyright file="SalesOrderConfirmedToPoHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.OrderManagement.Domain.Events;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="SalesOrderConfirmedEvent"/> and, for any drop-ship sales order
/// line, auto-creates a DropShip purchase order against the configured vendor. This is
/// the Sales -&gt; Purchasing leg of the integrated flow and completes the
/// Purchase + Inventory + Sales triangle: a confirmed drop-ship order triggers a
/// purchase order that, once received, flows inventory in (GoodsReceivedToInventoryHandler)
/// and is matched/invoiced through Accounts Payable.
///
/// The handler does not raise secondary domain events (it matches the other
/// cross-module handlers' pattern of writing directly to its own DbContext).
/// </summary>
public sealed class SalesOrderConfirmedToPoHandler : IDomainEventHandler<SalesOrderConfirmedEvent>
{
    private readonly PurchasingDbContext _purchasingContext;

    public SalesOrderConfirmedToPoHandler(PurchasingDbContext purchasingContext)
    {
        _purchasingContext = purchasingContext ?? throw new ArgumentNullException(nameof(purchasingContext));
    }

    public async Task HandleAsync(SalesOrderConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var dropShipLines = domainEvent.Lines
            .Where(l => l.IsDropShip && l.DropShipVendorId is not null)
            .ToList();

        if (dropShipLines.Count == 0)
            return;

        var salesOrderNumber = domainEvent.OrderNumber;
        var lineNo = 1;

        foreach (var line in dropShipLines)
        {
            var vendorId = line.DropShipVendorId!.Value;
            var itemIdString = line.ItemId.ToString();

            // Default the drop-ship PO unit cost from the vendor item catalogue when present.
            var vendorItem = await _purchasingContext.VendorItems
                .Where(v => v.VendorId == vendorId && v.ItemId == itemIdString)
                .OrderByDescending(v => v.IsPrimaryVendor)
                .FirstOrDefaultAsync(cancellationToken);

            var unitCost = vendorItem?.Cost ?? 0m;

            var po = new PurchaseOrder(
                poNumber: $"DS-{salesOrderNumber}-{lineNo}",
                companyId: domainEvent.CompanyId,
                vendorId: vendorId,
                orderDate: DateTime.UtcNow,
                orderType: PurchaseOrderType.DropShip,
                shipToName: "Drop-ship to customer",
                shipToAddress: null,
                paymentTermId: null,
                buyerId: null,
                buyerNotes: $"Auto-created from sales order {salesOrderNumber}.",
                vendorReference: salesOrderNumber);

            po.AddLine(new PurchaseOrderLine(
                purchaseOrderId: po.Id,
                lineNumber: lineNo,
                itemId: itemIdString,
                description: line.Description,
                quantity: line.Quantity,
                unitOfMeasure: line.UnitOfMeasure,
                unitPrice: unitCost,
                needByDate: null,
                accountId: line.AccountId,
                projectId: line.ProjectId,
                taskId: null,
                requisitionLineId: null));

            _purchasingContext.PurchaseOrders.Add(po);
            lineNo++;
        }

        if (_purchasingContext.ChangeTracker.HasChanges())
            await _purchasingContext.SaveChangesAsync(cancellationToken);
    }
}
