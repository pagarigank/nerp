// <copyright file="ProjectCostValidation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure;

/// <summary>
/// Implementation of the shared <see cref="IProjectCostValidation"/> contract (defined in
/// ERP.Core) consumed by Payroll when approving timesheets / expenses charged to a project.
/// Mirrors <c>CreditLimitCheckService</c>: the contract lives in ERP.Core so Payroll can
/// enforce project budget policy without a compile-time dependency on Project Accounting.
/// </summary>
public sealed class ProjectCostValidation : IProjectCostValidation
{
    private readonly ProjDbContext _context;

    public ProjectCostValidation(ProjDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ProjectCostValidationResult> ValidateAsync(
        Guid companyId,
        Guid? projectId,
        Guid? taskId,
        decimal proposedAmount,
        CancellationToken cancellationToken = default)
    {
        if (projectId is null)
            return new ProjectCostValidationResult(true, "No project charged; nothing to validate.", 0m, true, true);

        var project = await _context.Projects
            .Include(p => p.BudgetLines)
            .FirstOrDefaultAsync(p => p.Id == projectId.Value, cancellationToken);

        if (project is null)
            return new ProjectCostValidationResult(false, "Project does not exist.", 0m, false, false);

        var projectOpen = project.Status == ProjectStatus.Active && !project.IsClosed;
        if (!projectOpen)
        {
            return new ProjectCostValidationResult(false, $"Project is not open (status={project.Status}).", 0m, false, false);
        }

        // Match the budget line for the task (or the labor cost category if no task specified).
        var line = project.BudgetLines
            .Where(b => b.TaskId == (taskId ?? Guid.Empty))
            .OrderByDescending(b => b.RevisionNumber)
            .FirstOrDefault();

        if (line is null)
        {
            // No budget line defined for this task; allow but report zero remaining so callers
            // can decide. We treat absence of a budget line as a soft-pass (overhead/flexible).
            return new ProjectCostValidationResult(true, "No budget line defined for task; validation passed (unbudgeted).", 0m, true, true);
        }

        var remaining = line.Variance; // BudgetAmount - ActualAmount - CommittedAmount
        if (remaining < proposedAmount)
        {
            return new ProjectCostValidationResult(
                false,
                $"Proposed cost {proposedAmount:C} exceeds remaining budget {remaining:C} for this task.",
                remaining,
                true,
                true);
        }

        return new ProjectCostValidationResult(true, "Project budget validation passed.", remaining, true, true);
    }
}
