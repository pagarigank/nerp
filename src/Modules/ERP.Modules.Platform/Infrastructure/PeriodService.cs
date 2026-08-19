// <copyright file="PeriodService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Infrastructure;

public interface IPeriodService
{
    Task<FiscalPeriod?> GetCurrentPeriodAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> IsPeriodOpenAsync(Guid companyId, DateTimeOffset date, CancellationToken cancellationToken = default);
    Task ClosePeriodAsync(Guid periodId, string performedBy, IEnumerable<string> roles, CancellationToken cancellationToken = default);
    Task OpenPeriodAsync(Guid periodId, string performedBy, IEnumerable<string> roles, CancellationToken cancellationToken = default);
}

public class PeriodService : IPeriodService
{
    private readonly PlatformDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public PeriodService(PlatformDbContext context, IAuditLogService auditLogService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<FiscalPeriod?> GetCurrentPeriodAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalPeriods
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Status == PeriodStatus.Open, cancellationToken);
    }

    public async Task<bool> IsPeriodOpenAsync(Guid companyId, DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        return await _context.FiscalPeriods
            .AnyAsync(x => x.CompanyId == companyId
                && x.StartDate <= date
                && x.EndDate >= date
                && x.Status == PeriodStatus.Open,
                cancellationToken);
    }

    public async Task ClosePeriodAsync(Guid periodId, string performedBy, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var period = await _context.FiscalPeriods.FindAsync(new object[] { periodId }, cancellationToken)
            ?? throw new InvalidOperationException($"Fiscal period {periodId} not found.");

        if (period.Status != PeriodStatus.Open)
            throw new InvalidOperationException($"Fiscal period {periodId} is not open and cannot be closed.");

        var oldStatus = period.Status;
        period.Close();

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "PeriodClosed",
            nameof(FiscalPeriod),
            periodId,
            performedBy,
            new { Status = oldStatus.ToString() },
            new { Status = period.Status.ToString() },
            cancellationToken: cancellationToken);
    }

    public async Task OpenPeriodAsync(Guid periodId, string performedBy, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var period = await _context.FiscalPeriods.FindAsync(new object[] { periodId }, cancellationToken)
            ?? throw new InvalidOperationException($"Fiscal period {periodId} not found.");

        if (period.Status != PeriodStatus.Closed)
            throw new InvalidOperationException($"Fiscal period {periodId} is not closed and cannot be reopened.");

        var hasOpenPeriod = await _context.FiscalPeriods
            .AnyAsync(x => x.CompanyId == period.CompanyId && x.Status == PeriodStatus.Open, cancellationToken);

        if (hasOpenPeriod)
            throw new InvalidOperationException("Cannot open this period: another fiscal period is already open for this company. Close the current period first.");

        var isAdmin = roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "SystemAdmin", StringComparison.OrdinalIgnoreCase));

        if (!isAdmin)
            throw new UnauthorizedAccessException("Only an administrator can reopen a closed fiscal period.");

        var oldStatus = period.Status;
        period.Open();

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "PeriodReopened",
            nameof(FiscalPeriod),
            periodId,
            performedBy,
            new { Status = oldStatus.ToString() },
            new { Status = period.Status.ToString() },
            cancellationToken: cancellationToken);
    }
}
