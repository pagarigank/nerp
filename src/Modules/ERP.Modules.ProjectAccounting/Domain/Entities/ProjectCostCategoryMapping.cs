// <copyright file="ProjectCostCategoryMapping.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Per-company mapping from a project <see cref="CostCategory"/> to the General
/// Ledger account that receives the cost when a project cost is dual-posted.
/// This is the job-costing overlay (architecture.md §5.1): each project account
/// category maps to a GL expense/asset account so that a cost posted to the
/// project ledger automatically debits the right GL account. Enables the
/// "Cost transaction dual-posting service" (simultaneous GL + project ledger).
/// </summary>
public class ProjectCostCategoryMapping : AuditableEntity
{
    protected ProjectCostCategoryMapping() { }

    public ProjectCostCategoryMapping(
        Guid companyId,
        CostCategory costCategory,
        Guid glAccountId,
        string? description = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        CostCategory = costCategory;
        GlAccountId = glAccountId;
        Description = description;
    }

    public Guid CompanyId { get; private set; }

    public CostCategory CostCategory { get; private set; }

    public Guid GlAccountId { get; private set; }

    public string? Description { get; private set; }

    public void Update(Guid glAccountId, string? description = null)
    {
        GlAccountId = glAccountId;
        Description = description;
    }
}
