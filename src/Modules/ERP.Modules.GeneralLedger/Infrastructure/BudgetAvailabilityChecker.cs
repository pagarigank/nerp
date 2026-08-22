// <copyright file="BudgetAvailabilityChecker.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class BudgetAvailabilityChecker : IBudgetAvailabilityCheck
{
    private readonly GlDbContext _context;

    public BudgetAvailabilityChecker(GlDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<decimal> GetRemainingBudgetAsync(
        Guid companyId,
        Guid? projectId,
        Guid? glAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!glAccountId.HasValue && !projectId.HasValue)
            return 0m;

        var query =
            from line in _context.BudgetLines
            join budget in _context.Budgets on line.BudgetId equals budget.Id
            where budget.CompanyId == companyId && budget.IsActive
            select new { line.AccountId, line.ProjectId, line.Amount };

        if (glAccountId.HasValue)
            query = query.Where(x => x.AccountId == glAccountId.Value);

        if (projectId.HasValue)
            query = query.Where(x => x.ProjectId == projectId.Value);

        return await query.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
    }
}
