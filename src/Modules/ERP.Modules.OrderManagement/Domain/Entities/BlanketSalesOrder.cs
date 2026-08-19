// <copyright file="BlanketSalesOrder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Blanket / standing sales order (Phase 8 gap 583). A single agreement under one
/// order number authorising repeat deliveries up to a total quantity / value within
/// a validity window. Individual deliveries are created as <see cref="BlanketRelease"/>
/// records that draw down the remaining quantity and value.
/// </summary>
public class BlanketSalesOrder : AuditableAggregateRoot
{
    private readonly List<BlanketRelease> _releases = [];

    protected BlanketSalesOrder() { }

    public BlanketSalesOrder(
        string orderNumber,
        Guid companyId,
        Guid customerId,
        DateTime orderDate,
        decimal totalQuantity,
        decimal totalValue,
        DateTime validFrom,
        DateTime validTo,
        string? currency = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));
        if (totalQuantity <= 0)
            throw new ArgumentException("Total quantity must be positive.", nameof(totalQuantity));

        OrderNumber = orderNumber;
        CompanyId = companyId;
        CustomerId = customerId;
        OrderDate = orderDate;
        TotalQuantity = totalQuantity;
        TotalValue = totalValue;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Currency = currency;
        Status = BlanketOrderStatus.Active;
    }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public decimal TotalQuantity { get; private set; }
    public decimal TotalValue { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public string? Currency { get; private set; }
    public BlanketOrderStatus Status { get; private set; }

    public decimal ReleasedQuantity => _releases.Sum(r => r.Quantity);
    public decimal RemainingQuantity => TotalQuantity - ReleasedQuantity;
    public decimal ReleasedValue => _releases.Sum(r => r.Value);
    public decimal RemainingValue => TotalValue - ReleasedValue;

    public IReadOnlyCollection<BlanketRelease> Releases => _releases.AsReadOnly();

    public BlanketRelease AddRelease(decimal quantity, decimal value, DateTime releaseDate, string? reference = null)
    {
        if (Status != BlanketOrderStatus.Active)
            throw new InvalidOperationException($"Cannot add a release to a blanket order in {Status} status.");
        if (releaseDate < ValidFrom || releaseDate > ValidTo)
            throw new InvalidOperationException("Release date falls outside the blanket order validity window.");
        if (quantity <= 0)
            throw new ArgumentException("Release quantity must be positive.", nameof(quantity));
        if (ReleasedQuantity + quantity > TotalQuantity)
            throw new InvalidOperationException($"Release would exceed remaining blanket quantity ({RemainingQuantity}).");

        var release = new BlanketRelease(Id, quantity, value, releaseDate, reference);
        _releases.Add(release);
        return release;
    }

    public void Close() => Status = BlanketOrderStatus.Closed;

    public void Reopen() => Status = BlanketOrderStatus.Active;
}

public class BlanketRelease : AuditableEntity
{
    protected BlanketRelease() { }

    public BlanketRelease(Guid blanketOrderId, decimal quantity, decimal value, DateTime releaseDate, string? reference = null)
        : base(Guid.NewGuid())
    {
        BlanketOrderId = blanketOrderId;
        Quantity = quantity;
        Value = value;
        ReleaseDate = releaseDate;
        Reference = reference;
    }

    public Guid BlanketOrderId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Value { get; private set; }
    public DateTime ReleaseDate { get; private set; }
    public string? Reference { get; private set; }
    public Guid? CreatedSalesOrderId { get; private set; }

    public void LinkSalesOrder(Guid salesOrderId) => CreatedSalesOrderId = salesOrderId;
}

public enum BlanketOrderStatus
{
    Active = 0,
    Closed = 1,
    Expired = 2,
}
