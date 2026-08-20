// <copyright file="ServiceCall.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum ServiceCallStatus
{
    Logged,
    Triage,
    WorkOrderCreated,
    Closed
}

public class ServiceCall : AuditableEntity
{
    protected ServiceCall()
    {
    }

    public ServiceCall(
        Guid companyId,
        string callNumber,
        Guid? customerId,
        string? contactName,
        string? contactPhone,
        Guid? equipmentAssetId,
        Guid? serviceContractId,
        SlaPriority priority,
        string description,
        int responseMinutes)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        CallNumber = callNumber;
        CustomerId = customerId;
        ContactName = contactName;
        ContactPhone = contactPhone;
        EquipmentAssetId = equipmentAssetId;
        ServiceContractId = serviceContractId;
        Priority = priority;
        Description = description;
        Status = ServiceCallStatus.Logged;
        LoggedOn = DateTime.UtcNow;
        ResponseDue = LoggedOn.AddMinutes(responseMinutes);
    }

    public Guid CompanyId { get; private set; }
    public string CallNumber { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public string? ContactName { get; private set; }
    public string? ContactPhone { get; private set; }
    public Guid? EquipmentAssetId { get; private set; }
    public Guid? ServiceContractId { get; private set; }
    public SlaPriority Priority { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ServiceCallStatus Status { get; private set; }
    public DateTime LoggedOn { get; private set; }
    public DateTime? ResponseDue { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public string? ResolutionSummary { get; private set; }

    public void StartTriage() => Status = ServiceCallStatus.Triage;

    public void LinkWorkOrder(Guid workOrderId)
    {
        WorkOrderId = workOrderId;
        Status = ServiceCallStatus.WorkOrderCreated;
    }

    public void Close(string resolutionSummary)
    {
        ResolutionSummary = resolutionSummary;
        Status = ServiceCallStatus.Closed;
    }
}
