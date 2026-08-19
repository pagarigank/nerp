// <copyright file="ProjectRoleRate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Standard billing/cost rate per role (PM, engineer, laborer, ...). Drives
/// T&amp;M billing rate cards and labor cost rates by role (spec §7.4).
/// </summary>
public class ProjectRoleRate : AuditableEntity
{
    protected ProjectRoleRate() { }

    public ProjectRoleRate(
        Guid companyId,
        string roleName,
        decimal costRate,
        decimal billingRate,
        string? description = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name is required.", nameof(roleName));

        CompanyId = companyId;
        RoleName = roleName;
        CostRate = costRate;
        BillingRate = billingRate;
        Description = description;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string RoleName { get; private set; } = string.Empty;
    public decimal CostRate { get; private set; }
    public decimal BillingRate { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(decimal? costRate, decimal? billingRate, string? description, bool? isActive)
    {
        if (costRate.HasValue)
            CostRate = costRate.Value;
        if (billingRate.HasValue)
            BillingRate = billingRate.Value;
        if (description is not null)
            Description = description;
        if (isActive.HasValue)
            IsActive = isActive.Value;
    }
}
