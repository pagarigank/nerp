// <copyright file="PreventiveMaintenance.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum PmFrequency
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Semiannual,
    Annual
}

public class PreventiveMaintenance : AuditableEntity
{
    protected PreventiveMaintenance()
    {
    }

    public PreventiveMaintenance(
        Guid companyId,
        string code,
        string description,
        Guid? equipmentAssetId,
        Guid? serviceContractId,
        Guid? defaultTechnicianId,
        PmFrequency frequency,
        int intervalMonths,
        DateTime? lastGenerated,
        DateTime? nextDue,
        string? checklist,
        bool isActive)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = code;
        Description = description;
        EquipmentAssetId = equipmentAssetId;
        ServiceContractId = serviceContractId;
        DefaultTechnicianId = defaultTechnicianId;
        Frequency = frequency;
        IntervalMonths = intervalMonths;
        LastGenerated = lastGenerated;
        NextDue = nextDue;
        Checklist = checklist;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? EquipmentAssetId { get; private set; }
    public Guid? ServiceContractId { get; private set; }
    public Guid? DefaultTechnicianId { get; private set; }
    public PmFrequency Frequency { get; private set; }
    public int IntervalMonths { get; private set; }
    public DateTime? LastGenerated { get; private set; }
    public DateTime? NextDue { get; private set; }
    public string? Checklist { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void MarkGenerated(DateTime when)
    {
        LastGenerated = when;
        NextDue = when.AddMonths(IntervalMonths);
    }

    public void Deactivate() => IsActive = false;
}
