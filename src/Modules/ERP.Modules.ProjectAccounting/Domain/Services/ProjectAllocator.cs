// <copyright file="ProjectAllocator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Domain.Services;

/// <summary>
/// Markup / burden calculation engine (spec §7.3 Project Allocator). Resolves
/// the best-matching active allocation rule for a cost category and computes the
/// total burdened (billable) cost for a base cost amount.
/// </summary>
public interface IProjectAllocator
{
    Task<AllocationResult> CalculateAsync(Guid projectId, CostCategory category, decimal baseCost, CancellationToken cancellationToken);
}

public class AllocationResult
{
    public CostCategory Category { get; init; }
    public decimal BaseCost { get; init; }
    public decimal Overhead { get; init; }
    public decimal Markup { get; init; }
    public decimal Burden { get; init; }
    public decimal BillableCost => BaseCost + Burden;
    public decimal MarkupPercentage { get; init; }
    public decimal OverheadPercentage { get; init; }
}

public class ProjectAllocator : IProjectAllocator
{
    private readonly ProjDbContext _context;

    public ProjectAllocator(ProjDbContext context)
    {
        _context = context;
    }

    public async Task<AllocationResult> CalculateAsync(Guid projectId, CostCategory category, decimal baseCost, CancellationToken cancellationToken)
    {
        var rule = await _context.ProjectAllocationRules
            .Where(r => r.ProjectId == projectId && r.IsActive && r.Category == category)
            .OrderBy(r => r.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        var overheadPct = rule?.OverheadPercentage ?? 0m;
        var markupPct = rule?.MarkupPercentage ?? 0m;

        var overhead = baseCost * overheadPct / 100m;
        var markup = (baseCost + overhead) * markupPct / 100m;
        var burden = overhead + markup;

        return new AllocationResult
        {
            Category = category,
            BaseCost = baseCost,
            Overhead = overhead,
            Markup = markup,
            Burden = burden,
            MarkupPercentage = markupPct,
            OverheadPercentage = overheadPct,
        };
    }
}
