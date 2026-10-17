// <copyright file="SalesCall.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class SalesCall : AuditableAggregateRoot
{
    private readonly List<SalesCallLine> _lines = new();

    private SalesCall() { }

    public SalesCall(
        Guid companyId,
        string callNumber,
        Guid salesRepId,
        Guid customerId,
        Guid? routeId,
        DateTime callDate,
        DateTime startTime,
        string? notes = null,
        bool isOfflineCreated = false,
        string? clientCallId = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        CallNumber = callNumber;
        SalesRepId = salesRepId;
        CustomerId = customerId;
        RouteId = routeId;
        CallDate = callDate;
        StartTime = startTime;
        Status = SalesCallStatus.InProgress;
        Notes = notes;
        IsOfflineCreated = isOfflineCreated;
        ClientCallId = clientCallId;
        TotalAmount = 0m;
    }

    public Guid CompanyId { get; private set; }

    public string CallNumber { get; private set; } = string.Empty;

    public Guid SalesRepId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid? RouteId { get; private set; }

    public DateTime CallDate { get; private set; }

    public DateTime StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public SalesCallStatus Status { get; private set; } = SalesCallStatus.Scheduled;

    public string? Notes { get; private set; }

    public Guid? SalesOrderId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public bool IsOfflineCreated { get; private set; }

    public string? ClientCallId { get; private set; }

    public DateTime? SyncedAt { get; private set; }

    public IReadOnlyCollection<SalesCallLine> Lines => _lines.AsReadOnly();

    public void AddLine(SalesCallLine line)
    {
        _lines.Add(line);
        RecalculateTotal();
    }

    public void ClearLines()
    {
        _lines.Clear();
        TotalAmount = 0m;
    }

    public void CompleteCall(DateTime endTime, string? notes = null)
    {
        EndTime = endTime;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes;
        }

        Status = SalesCallStatus.Completed;
        RecalculateTotal();
    }

    public void CancelCall(string? reason = null)
    {
        EndTime = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = (Notes != null ? Notes + " | " : string.Empty) + "Cancelled: " + reason;
        }

        Status = SalesCallStatus.Cancelled;
    }

    public void LinkSalesOrder(Guid salesOrderId)
    {
        SalesOrderId = salesOrderId;
    }

    public void MarkSynced(DateTime syncedAt)
    {
        SyncedAt = syncedAt;
    }

    public void RecalculateTotal()
    {
        TotalAmount = _lines.Sum(l => l.LineTotal);
    }
}
