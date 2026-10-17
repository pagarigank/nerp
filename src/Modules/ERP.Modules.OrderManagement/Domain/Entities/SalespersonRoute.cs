// <copyright file="SalespersonRoute.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class SalespersonRoute : AuditableEntity
{
    private SalespersonRoute() { }

    public SalespersonRoute(
        Guid companyId,
        Guid salesRepId,
        Guid customerId,
        DayOfWeek dayOfWeek,
        int sequenceOrder,
        string? estimatedTime = null,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        SalesRepId = salesRepId;
        CustomerId = customerId;
        DayOfWeek = dayOfWeek;
        SequenceOrder = sequenceOrder;
        EstimatedTime = estimatedTime;
        Notes = notes;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public Guid SalesRepId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public int SequenceOrder { get; private set; }

    public string? EstimatedTime { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(
        DayOfWeek dayOfWeek,
        int sequenceOrder,
        string? estimatedTime,
        string? notes,
        bool isActive)
    {
        DayOfWeek = dayOfWeek;
        SequenceOrder = sequenceOrder;
        EstimatedTime = estimatedTime;
        Notes = notes;
        IsActive = isActive;
    }

    public void SetSequence(int sequenceOrder)
    {
        SequenceOrder = sequenceOrder;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
