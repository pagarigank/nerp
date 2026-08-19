// <copyright file="EmployeeProjectAssignment.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Employee assignment to a project/task with allocation % and effective dates
/// (spec §7.4 resource staffing).
/// </summary>
public class EmployeeProjectAssignment : AuditableEntity
{
    protected EmployeeProjectAssignment() { }

    public EmployeeProjectAssignment(
        Guid companyId,
        Guid projectId,
        Guid? taskId,
        string employeeId,
        string roleName,
        decimal allocationPercentage,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            throw new ArgumentException("Employee id is required.", nameof(employeeId));

        CompanyId = companyId;
        ProjectId = projectId;
        TaskId = taskId;
        EmployeeId = employeeId;
        RoleName = roleName;
        AllocationPercentage = allocationPercentage;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public string EmployeeId { get; private set; } = string.Empty;
    public string RoleName { get; private set; } = string.Empty;
    public decimal AllocationPercentage { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(Guid? taskId, string? roleName, decimal? allocationPercentage, DateTime? effectiveTo, bool? isActive)
    {
        if (taskId.HasValue)
            TaskId = taskId;
        if (!string.IsNullOrWhiteSpace(roleName))
            RoleName = roleName;
        if (allocationPercentage.HasValue)
            AllocationPercentage = allocationPercentage.Value;
        if (effectiveTo.HasValue)
            EffectiveTo = effectiveTo.Value;
        if (isActive.HasValue)
            IsActive = isActive.Value;
    }
}
