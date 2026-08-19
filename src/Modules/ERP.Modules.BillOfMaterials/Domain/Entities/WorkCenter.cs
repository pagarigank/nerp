// <copyright file="WorkCenter.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class WorkCenter : AuditableEntity
{
    protected WorkCenter() { }

    public WorkCenter(
        Guid companyId,
        string code,
        string name,
        string? department,
        decimal capacityHoursPerDay,
        decimal efficiencyPercentage,
        decimal costRatePerHour,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Work center code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Work center name is required.", nameof(name));

        CompanyId = companyId;
        Code = code;
        Name = name;
        Department = department;
        CapacityHoursPerDay = capacityHoursPerDay;
        EfficiencyPercentage = efficiencyPercentage;
        CostRatePerHour = costRatePerHour;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Department { get; private set; }
    public decimal CapacityHoursPerDay { get; private set; }
    public decimal EfficiencyPercentage { get; private set; }
    public decimal CostRatePerHour { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string? name,
        string? department,
        decimal? capacityHoursPerDay,
        decimal? efficiencyPercentage,
        decimal? costRatePerHour,
        bool? isActive)
    {
        if (name is not null)
        {
            Name = name;
        }

        if (department is not null)
        {
            Department = department;
        }

        if (capacityHoursPerDay.HasValue)
        {
            CapacityHoursPerDay = capacityHoursPerDay.Value;
        }

        if (efficiencyPercentage.HasValue)
        {
            EfficiencyPercentage = efficiencyPercentage.Value;
        }

        if (costRatePerHour.HasValue)
        {
            CostRatePerHour = costRatePerHour.Value;
        }

        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
    }
}
