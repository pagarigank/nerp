// <copyright file="EacSnapshotService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.ProjectAccounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure;

public interface IEacSnapshotService
{
    Task<int> CaptureForAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Captures one estimate-at-completion snapshot per active project per UTC day using the same
/// forecast math as the analysis forecast endpoint (budget falls back revised → original →
/// contract value; EAC = costs-to-date ÷ physical % complete, budget when no progress is
/// reported). An existing snapshot for the same project + UTC day is refreshed in place.
/// </summary>
public sealed class EacSnapshotService : IEacSnapshotService
{
    private readonly ProjDbContext _context;

    public EacSnapshotService(ProjDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> CaptureForAllAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var nextDayStart = dayStart.AddDays(1);

        var projects = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .Include(p => p.BudgetLines)
            .Include(p => p.ChangeOrders)
            .ToListAsync(cancellationToken);

        var existingToday = await _context.ProjectEacSnapshots
            .Where(s => s.CapturedOn >= dayStart && s.CapturedOn < nextDayStart)
            .ToListAsync(cancellationToken);

        var captured = 0;
        foreach (var project in projects)
        {
            var budgetAtCompletion = project.BudgetLines.Sum(b => b.BudgetAmount);
            if (budgetAtCompletion <= 0)
            {
                budgetAtCompletion = project.RevisedBudget;
            }

            if (budgetAtCompletion <= 0)
            {
                budgetAtCompletion = project.ContractValue ?? 0;
            }

            decimal estimateAtCompletion;
            if (project.EstimateAtCompletion > 0)
            {
                estimateAtCompletion = project.EstimateAtCompletion;
            }
            else if (project.PercentComplete > 0)
            {
                estimateAtCompletion = project.CostsToDate / (project.PercentComplete / 100m);
            }
            else
            {
                estimateAtCompletion = budgetAtCompletion;
            }

            var marginBase = project.ContractValue ?? budgetAtCompletion;
            var estimatedMarginPct = marginBase > 0
                ? decimal.Round((marginBase - estimateAtCompletion) / marginBase * 100, 4)
                : 0m;

            var pendingChangeOrders = project.ChangeOrders
                .Where(c => c.Status == ChangeOrderStatus.Draft || c.Status == ChangeOrderStatus.Submitted)
                .Sum(c => c.Amount);

            var snapshot = existingToday.FirstOrDefault(s => s.ProjectId == project.Id);
            if (snapshot is null)
            {
                _context.ProjectEacSnapshots.Add(new ProjectEacSnapshot(
                    project.CompanyId,
                    project.Id,
                    now,
                    budgetAtCompletion,
                    estimateAtCompletion,
                    estimatedMarginPct,
                    pendingChangeOrders));
                captured++;
            }
            else
            {
                snapshot.UpdateValues(budgetAtCompletion, estimateAtCompletion, estimatedMarginPct, pendingChangeOrders);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return captured;
    }
}
