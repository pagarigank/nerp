// <copyright file="Estimate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum EstimateStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Converted
}

public class Estimate : AuditableEntity
{
    protected Estimate()
    {
    }

    public Estimate(
        Guid companyId,
        string estimateNumber,
        Guid? customerId,
        Guid? serviceContractId,
        Guid? equipmentAssetId,
        BillingType billingType,
        decimal laborEstimate,
        decimal partsEstimate,
        decimal travelEstimate,
        decimal taxEstimate,
        string? notes)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        EstimateNumber = estimateNumber;
        CustomerId = customerId;
        ServiceContractId = serviceContractId;
        EquipmentAssetId = equipmentAssetId;
        Status = EstimateStatus.Draft;
        BillingType = billingType;
        LaborEstimate = laborEstimate;
        PartsEstimate = partsEstimate;
        TravelEstimate = travelEstimate;
        TaxEstimate = taxEstimate;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }
    public string EstimateNumber { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public Guid? ServiceContractId { get; private set; }
    public Guid? EquipmentAssetId { get; private set; }
    public EstimateStatus Status { get; private set; }
    public BillingType BillingType { get; private set; }
    public decimal LaborEstimate { get; private set; }
    public decimal PartsEstimate { get; private set; }
    public decimal TravelEstimate { get; private set; }
    public decimal TaxEstimate { get; private set; }
    public decimal TotalEstimate => LaborEstimate + PartsEstimate + TravelEstimate + TaxEstimate;
    public string? Notes { get; private set; }

    public void Submit() => Status = EstimateStatus.Submitted;

    public void Approve() => Status = EstimateStatus.Approved;

    public void Reject() => Status = EstimateStatus.Rejected;

    public void MarkConverted() => Status = EstimateStatus.Converted;
}
