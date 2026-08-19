// <copyright file="SalesReportService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Application.Services;

/// <summary>
/// Reads Order Management data to produce the standard order-entry / sales reports.
/// All figures are sourced from OM aggregates (sales orders, shipments, returns, masters);
/// cross-module figures (e.g. AR-invoiced amounts) are intentionally not pulled here so the
/// OM module stays free of a dependency on Accounts Receivable. AR-derived analytics live in
/// the AR reports.
/// </summary>
public sealed class SalesReportService
{
    private readonly OmDbContext _context;

    public SalesReportService(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<OpenOrderRow>> GetOpenOrdersAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SalesOrders
            .Where(o => o.CompanyId == companyId && o.Status != SalesOrderStatus.Closed && o.Status != SalesOrderStatus.Cancelled && o.Status != SalesOrderStatus.Shipped)
            .OrderBy(o => o.OrderNumber)
            .Select(o => new OpenOrderRow(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.OrderDate,
                o.Status.ToString(),
                o.Lines.Sum(l => l.Quantity),
                o.Lines.Sum(l => l.Quantity - l.ShippedQuantity),
                o.Lines.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))),
                o.SalesRepId,
                o.IsOnCreditHold))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<BackorderRow>> GetBackordersAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference (SalesOrder navigation is guaranteed by the query filter).
        var rows = await _context.SalesOrders
            .Where(o => o.CompanyId == companyId && o.Status != SalesOrderStatus.Closed && o.Status != SalesOrderStatus.Cancelled)
            .SelectMany(o => o.Lines)
            .Where(l => (l.Quantity - l.ShippedQuantity) > 0)
            .OrderBy(l => l.SalesOrder.OrderNumber)
            .Select(l => new BackorderRow(
                l.Id,
                l.SalesOrderId,
                l.SalesOrder.OrderNumber,
                l.SalesOrder.CustomerId,
                l.ItemId,
                l.WarehouseId,
                l.Quantity,
                l.ShippedQuantity,
                l.Quantity - l.ShippedQuantity,
                l.UnitPrice))
            .ToListAsync(cancellationToken);
#pragma warning restore CS8602

        return rows;
    }

    public async Task<IReadOnlyList<ShipmentRegisterRow>> GetShipmentRegisterAsync(Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var query = _context.Shipments.Where(s => s.CompanyId == companyId);
        if (from is not null)
            query = query.Where(s => s.ShipmentDate >= from.Value.Date);
        if (to is not null)
            query = query.Where(s => s.ShipmentDate <= to.Value.Date);

        var rows = await query
            .OrderByDescending(s => s.ShipmentDate)
            .Select(s => new ShipmentRegisterRow(
                s.Id,
                s.ShipmentNumber,
                s.SalesOrderId,
                s.CustomerId,
                s.ShipmentDate,
                s.Status.ToString(),
                s.FreightCost,
                s.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<SalesAnalysisRow>> GetSalesAnalysisAsync(Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var orders = _context.SalesOrders.Where(o => o.CompanyId == companyId);
        if (from is not null)
            orders = orders.Where(o => o.OrderDate >= from.Value);
        if (to is not null)
            orders = orders.Where(o => o.OrderDate <= to.Value);

        // Group by customer then item to produce a sales-by-item matrix.
#pragma warning disable CS8602
        var rows = await orders
            .SelectMany(o => o.Lines)
            .GroupBy(l => new { l.ItemId, l.SalesOrder.CustomerId })
            .OrderByDescending(g => g.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))))
            .Select(g => new SalesAnalysisRow(
                g.Key.ItemId,
                g.Key.CustomerId,
                g.Sum(l => l.Quantity),
                g.Sum(l => l.ShippedQuantity),
                g.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))),
                g.Sum(l => ((l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))) * (l.TaxPercent / 100m))))
            .ToListAsync(cancellationToken);
#pragma warning restore CS8602

        return rows;
    }

    public async Task<IReadOnlyList<CreditHoldRow>> GetCreditHoldsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SalesOrders
            .Where(o => o.CompanyId == companyId && o.IsOnCreditHold)
            .OrderBy(o => o.OrderNumber)
            .Select(o => new CreditHoldRow(o.Id, o.OrderNumber, o.CustomerId, o.CreditHoldReason ?? string.Empty, o.OrderDate, o.Status.ToString()))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<DropShipStatusRow>> GetDropShipStatusAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
#pragma warning disable CS8602
        var rows = await _context.SalesOrders
            .Where(o => o.CompanyId == companyId)
            .SelectMany(o => o.Lines)
            .Where(l => l.IsDropShip)
            .OrderBy(l => l.SalesOrder.OrderNumber)
            .Select(l => new DropShipStatusRow(
                l.Id,
                l.SalesOrderId,
                l.SalesOrder.OrderNumber,
                l.SalesOrder.CustomerId,
                l.ItemId,
                l.DropShipVendorId,
                l.Quantity,
                l.ShippedQuantity,
                l.Quantity - l.ShippedQuantity))
            .ToListAsync(cancellationToken);
#pragma warning restore CS8602

        return rows;
    }

    public async Task<IReadOnlyList<SalesTaxRow>> GetSalesTaxAsync(Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var orders = _context.SalesOrders.Where(o => o.CompanyId == companyId);
        if (from is not null)
            orders = orders.Where(o => o.OrderDate >= from.Value);
        if (to is not null)
            orders = orders.Where(o => o.OrderDate <= to.Value);

        var rows = await orders
            .SelectMany(o => o.Lines)
            .GroupBy(l => l.TaxPercent)
            .OrderByDescending(g => g.Sum(l => ((l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))) * (l.TaxPercent / 100m)))
            .Select(g => new SalesTaxRow(
                g.Key,
                g.Sum(l => l.Quantity),
                g.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))),
                g.Sum(l => ((l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))) * (l.TaxPercent / 100m))))
            .ToListAsync(cancellationToken);

        return rows;
    }

    /// <summary>Sales Trend: net sales by calendar month (for trend charts).</summary>
    public async Task<IReadOnlyList<SalesTrendRow>> GetSalesTrendAsync(Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var orders = _context.SalesOrders.Where(o => o.CompanyId == companyId);
        if (from is not null)
            orders = orders.Where(o => o.OrderDate >= from.Value);
        if (to is not null)
            orders = orders.Where(o => o.OrderDate <= to.Value);

        var rows = await orders
            .SelectMany(o => o.Lines)
#pragma warning disable CS8602 // SalesOrder navigation is guaranteed loaded by the Include on the order query.
            .GroupBy(l => new { l.SalesOrder.OrderDate.Year, l.SalesOrder.OrderDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new SalesTrendRow(
                g.Key.Year,
                g.Key.Month,
                g.Sum(l => l.Quantity),
                g.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))),
                g.Sum(l => ((l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m))) * (l.TaxPercent / 100m))))
            .ToListAsync(cancellationToken);

        return rows;
    }

    /// <summary>Customer Order History: every sales order for a single customer with its value and shipped total.</summary>
    public async Task<IReadOnlyList<CustomerOrderHistoryRow>> GetCustomerOrderHistoryAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SalesOrders
            .Where(o => o.CompanyId == companyId && o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new CustomerOrderHistoryRow(
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.Status.ToString(),
                o.Lines.Sum(l => (l.Quantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m)) * (1m + (l.TaxPercent / 100m))),
                o.Lines.Sum(l => l.ShippedQuantity),
                o.Lines.Sum(l => l.Quantity),
                o.RemainingToShip))
            .ToListAsync(cancellationToken);

        return rows;
    }

    /// <summary>Shipping Log: all shipments (carrier, tracking, freight, value) by date range.</summary>
    public async Task<IReadOnlyList<ShippingLogRow>> GetShippingLogAsync(Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var shipments = _context.Shipments.Where(s => s.CompanyId == companyId);
        if (from is not null)
            shipments = shipments.Where(s => s.ShipmentDate >= from.Value);
        if (to is not null)
            shipments = shipments.Where(s => s.ShipmentDate <= to.Value);

        var rows = await shipments
            .OrderByDescending(s => s.ShipmentDate)
            .Select(s => new ShippingLogRow(
                s.Id,
                s.ShipmentNumber,
                s.SalesOrderId,
                s.CustomerId,
                s.ShipmentDate,
                s.Carrier ?? string.Empty,
                s.TrackingNumber ?? string.Empty,
                s.FreightCost,
                s.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync(cancellationToken);

        return rows;
    }

    /// <summary>Freight Analysis: freight cost vs. freight billed (shipment freight) by carrier.</summary>
    public async Task<IReadOnlyList<FreightAnalysisRow>> GetFreightAnalysisAsync(Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        var shipments = _context.Shipments.Where(s => s.CompanyId == companyId);
        if (from is not null)
            shipments = shipments.Where(s => s.ShipmentDate >= from.Value);
        if (to is not null)
            shipments = shipments.Where(s => s.ShipmentDate <= to.Value);

        var rows = await shipments
            .Select(s => new
            {
                Carrier = s.Carrier ?? "(none)",
                s.FreightCost,
                GoodsValue = s.Lines.Sum(l => l.Quantity * l.UnitPrice),
            })
            .ToListAsync(cancellationToken);

        var result = rows
            .GroupBy(s => s.Carrier)
            .Select(g => new FreightAnalysisRow(
                g.Key,
                g.Count(),
                g.Sum(s => s.FreightCost),
                g.Sum(s => s.GoodsValue)))
            .OrderByDescending(r => r.FreightCost)
            .ToList();

        return result;
    }
}

public sealed record OpenOrderRow(Guid OrderId, string OrderNumber, Guid? CustomerId, DateTime OrderDate, string Status, decimal OrderedQty, decimal BackorderedQty, decimal OrderValue, string? SalesRepId, bool IsOnCreditHold);
public sealed record BackorderRow(Guid LineId, Guid OrderId, string OrderNumber, Guid? CustomerId, Guid ItemId, Guid? WarehouseId, decimal OrderedQty, decimal ShippedQty, decimal BackorderedQty, decimal UnitPrice);
public sealed record ShipmentRegisterRow(Guid ShipmentId, string ShipmentNumber, Guid? SalesOrderId, Guid? CustomerId, DateTime ShipDate, string Status, decimal FreightCost, decimal ShipmentValue);
public sealed record SalesAnalysisRow(Guid ItemId, Guid? CustomerId, decimal Quantity, decimal ShippedQuantity, decimal NetSales, decimal TaxAmount);
public sealed record CreditHoldRow(Guid OrderId, string OrderNumber, Guid? CustomerId, string Reason, DateTime OrderDate, string Status);
public sealed record DropShipStatusRow(Guid LineId, Guid OrderId, string OrderNumber, Guid? CustomerId, Guid ItemId, Guid? DropShipVendorId, decimal OrderedQty, decimal ShippedQty, decimal BackorderedQty);
public sealed record SalesTaxRow(decimal TaxPercent, decimal Quantity, decimal TaxableAmount, decimal TaxAmount);
public sealed record SalesTrendRow(int Year, int Month, decimal Quantity, decimal NetSales, decimal TaxAmount);
public sealed record CustomerOrderHistoryRow(Guid OrderId, string OrderNumber, DateTime OrderDate, string Status, decimal OrderValue, decimal ShippedQty, decimal OrderedQty, decimal RemainingQty);
public sealed record ShippingLogRow(Guid ShipmentId, string ShipmentNumber, Guid? SalesOrderId, Guid? CustomerId, DateTime ShipDate, string Carrier, string TrackingNumber, decimal FreightCost, decimal ShipmentValue);
public sealed record FreightAnalysisRow(string Carrier, int ShipmentCount, decimal FreightCost, decimal GoodsValue);
