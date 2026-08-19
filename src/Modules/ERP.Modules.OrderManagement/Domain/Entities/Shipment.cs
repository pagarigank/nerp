// <copyright file="Shipment.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Events;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Shipment fulfils one or more sales-order lines. Confirming a shipment emits a
/// <see cref="ShipmentConfirmedEvent"/> consumed by the Inventory module (stock
/// relief / COGS) and the AR module (customer invoice generation -> GL).
/// </summary>
public class Shipment : AuditableAggregateRoot
{
    private readonly List<ShipmentLine> _lines = [];

    protected Shipment() { }

    public Shipment(
        string shipmentNumber,
        Guid companyId,
        Guid customerId,
        Guid? salesOrderId,
        DateTime shipmentDate,
        string? carrier = null,
        string? trackingNumber = null,
        decimal freightCost = 0)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(shipmentNumber))
            throw new ArgumentException("Shipment number is required.", nameof(shipmentNumber));

        ShipmentNumber = shipmentNumber;
        CompanyId = companyId;
        CustomerId = customerId;
        SalesOrderId = salesOrderId;
        ShipmentDate = shipmentDate;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        FreightCost = freightCost;
        Status = ShipmentStatus.Draft;
    }

    public string ShipmentNumber { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public DateTime ShipmentDate { get; private set; }
    public string? Carrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public decimal FreightCost { get; private set; }
    public ShipmentStatus Status { get; private set; }

    public IReadOnlyCollection<ShipmentLine> Lines => _lines.AsReadOnly();

    public void AddLine(ShipmentLine line)
    {
        if (Status != ShipmentStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a non-draft shipment.");
        _lines.Add(line);
    }

    public void Confirm()
    {
        if (Status != ShipmentStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm shipment in {Status} status.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot confirm a shipment with no lines.");

        Status = ShipmentStatus.Confirmed;
        AddDomainEvent(new ShipmentConfirmedEvent(
            Id, ShipmentNumber, CompanyId, CustomerId, SalesOrderId, ShipmentDate, Carrier, TrackingNumber, FreightCost, Lines.ToList()));
    }
}

public enum ShipmentStatus
{
    Draft = 0,
    Confirmed = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4,
}
