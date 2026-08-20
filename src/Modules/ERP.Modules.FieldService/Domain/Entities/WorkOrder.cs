// <copyright file="WorkOrder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Generic;
using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum WorkOrderStatus
{
    Draft,
    Scheduled,
    Dispatched,
    InProgress,
    OnHold,
    Completed,
    Closed,
    Cancelled
}

public enum WorkOrderType
{
    Repair,
    Install,
    Inspection,
    PreventiveMaintenance,
    Warranty
}

public enum WorkOrderLineType
{
    Labor,
    Part,
    Travel,
    Fee,
    Expense
}

public class WorkOrder : AuditableEntity
{
    private readonly List<WorkOrderLine> _lines = new ();

    protected WorkOrder()
    {
    }

    public WorkOrder(
        Guid companyId,
        string workOrderNumber,
        Guid? serviceCallId,
        Guid? estimateId,
        Guid? serviceContractId,
        Guid? equipmentAssetId,
        Guid? customerId,
        Guid? preventiveMaintenanceId,
        WorkOrderType type,
        SlaPriority priority,
        Guid? technicianId,
        Guid? territoryId,
        DateTime? scheduledStart,
        DateTime? scheduledEnd,
        DateTime? responseDue,
        DateTime? resolutionDue,
        bool warrantyCovered,
        string? notes)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        WorkOrderNumber = workOrderNumber;
        ServiceCallId = serviceCallId;
        EstimateId = estimateId;
        ServiceContractId = serviceContractId;
        EquipmentAssetId = equipmentAssetId;
        CustomerId = customerId;
        PreventiveMaintenanceId = preventiveMaintenanceId;
        Type = type;
        Status = WorkOrderStatus.Draft;
        Priority = priority;
        TechnicianId = technicianId;
        TerritoryId = territoryId;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        ResponseDue = responseDue;
        ResolutionDue = resolutionDue;
        WarrantyCovered = warrantyCovered;
        Fees = 0m;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }

    public string WorkOrderNumber { get; private set; } = string.Empty;

    public Guid? ServiceCallId { get; private set; }

    public Guid? EstimateId { get; private set; }

    public Guid? ServiceContractId { get; private set; }

    public Guid? EquipmentAssetId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? PreventiveMaintenanceId { get; private set; }

    public WorkOrderType Type { get; private set; }

    public WorkOrderStatus Status { get; private set; }

    public SlaPriority Priority { get; private set; }

    public Guid? TechnicianId { get; private set; }

    public Guid? TerritoryId { get; private set; }

    public DateTime? ScheduledStart { get; private set; }

    public DateTime? ScheduledEnd { get; private set; }

    public DateTime? ResponseDue { get; private set; }

    public DateTime? ResolutionDue { get; private set; }

    public DateTime? ClockIn { get; private set; }

    public DateTime? ClockOut { get; private set; }

    public decimal LaborHours { get; private set; }

    public decimal LaborCost { get; private set; }

    public decimal PartsCost { get; private set; }

    public decimal TravelCost { get; private set; }

    public decimal Fees { get; private set; }

    public decimal BillableTotal { get; private set; }

    public bool WarrantyCovered { get; private set; }

    public bool BilledToAr { get; private set; }

    public bool SlaBreached { get; private set; }

    public Guid? ArInvoiceId { get; private set; }

    public string? Notes { get; private set; }

    public string? Resolution { get; private set; }

    public IReadOnlyCollection<WorkOrderLine> Lines => _lines.AsReadOnly();

    public void Schedule(DateTime start, DateTime end, Guid? technicianId)
    {
        ScheduledStart = start;
        ScheduledEnd = end;
        TechnicianId = technicianId ?? TechnicianId;
        Status = WorkOrderStatus.Scheduled;
    }

    public void Dispatch(Guid technicianId, Guid? territoryId = null)
    {
        TechnicianId = technicianId;
        if (territoryId.HasValue)
        {
            TerritoryId = territoryId;
        }

        Status = WorkOrderStatus.Dispatched;
    }

    public void ClockInTech()
    {
        ClockIn = DateTime.UtcNow;
        Status = WorkOrderStatus.InProgress;
    }

    public void ClockOutTech(decimal laborHours, decimal laborCost)
    {
        ClockOut = DateTime.UtcNow;
        LaborHours = laborHours;
        LaborCost = laborCost;
    }

    public void Hold() => Status = WorkOrderStatus.OnHold;

    public void Resume() => Status = WorkOrderStatus.InProgress;

    public void Complete(string? resolution)
    {
        Resolution = resolution;
        Status = WorkOrderStatus.Completed;
    }

    public void Close()
    {
        if (Status != WorkOrderStatus.Completed)
        {
            throw new InvalidOperationException("Work order must be Completed before closing.");
        }

        Status = WorkOrderStatus.Closed;
    }

    public void Cancel() => Status = WorkOrderStatus.Cancelled;

    public void MarkSlaBreached() => SlaBreached = true;

    public void AddLine(WorkOrderLine line) => _lines.Add(line);

    public void RecalculateBillable(decimal partsMarkupPercent, decimal tripCharge)
    {
        PartsCost = _lines
            .Where(l => l.LineType == WorkOrderLineType.Part)
            .Sum(l => l.LineTotal);
        var multiplier = 1m + (partsMarkupPercent / 100m);
        var markedUpParts = PartsCost * multiplier;
        TravelCost = tripCharge;
        BillableTotal = LaborCost + markedUpParts + TravelCost + Fees;
    }

    public void MarkBilledToAr(Guid arInvoiceId)
    {
        ArInvoiceId = arInvoiceId;
        BilledToAr = true;
    }
}

public class WorkOrderLine : AuditableEntity
{
    protected WorkOrderLine()
    {
    }

    public WorkOrderLine(
        Guid workOrderId,
        WorkOrderLineType lineType,
        string description,
        decimal quantity,
        decimal unitRate,
        bool billable,
        Guid? itemId = null,
        Guid? technicianId = null)
    {
        Id = Guid.NewGuid();
        WorkOrderId = workOrderId;
        LineType = lineType;
        Description = description;
        Quantity = quantity;
        UnitRate = unitRate;
        Billable = billable;
        ItemId = itemId;
        TechnicianId = technicianId;
        LineTotal = quantity * unitRate;
        CostAmount = lineType == WorkOrderLineType.Part ? (quantity * unitRate) : 0m;
    }

    public Guid WorkOrderId { get; private set; }

    public WorkOrderLineType LineType { get; private set; }

    public Guid? ItemId { get; private set; }

    public Guid? TechnicianId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitRate { get; private set; }

    public decimal LineTotal { get; private set; }

    public bool Billable { get; private set; } = true;

    public decimal CostAmount { get; private set; }
}
