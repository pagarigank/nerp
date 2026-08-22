// <copyright file="CommissionRunJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.OrderManagement.Infrastructure.Jobs;

public interface ICommissionRunJob
{
    Task<CommissionRunReport> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record CommissionRunReport(
    Guid? RunId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int RepCount,
    decimal TotalRevenue,
    decimal TotalCommission,
    bool SkippedDuplicate);

/// <summary>
/// Weekly commission calculation: computes the previous ISO week's shipped-line
/// revenue per sales rep (orders dated in the window with lines where shipped
/// quantity is positive, valued net of line discount), snapshots each rep's
/// commission rate and persists a <see cref="CommissionRun"/> header plus one
/// <see cref="CommissionRunLine"/> per rep. A run for a period already on record
/// (unique PeriodStart + rep index) is skipped, so the job is safe to re-run.
/// </summary>
public class CommissionRunJob : ICommissionRunJob
{
    private readonly OmDbContext _context;
    private readonly ILogger<CommissionRunJob> _logger;

    public CommissionRunJob(OmDbContext context, ILogger<CommissionRunJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CommissionRunReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var (periodStart, periodEndExclusive) = PreviousIsoWeekUtc();
        var reportPeriodEnd = periodEndExclusive.AddTicks(-1);

        var duplicate = await _context.CommissionRuns
            .AnyAsync(r => r.PeriodStart == periodStart, cancellationToken);
        if (duplicate)
        {
            _logger.LogInformation(
                "Commission run for week {PeriodStart:yyyy-MM-dd} already exists; skipped.",
                periodStart);
            return new CommissionRunReport(null, periodStart, reportPeriodEnd, 0, 0m, 0m, SkippedDuplicate: true);
        }

        var reps = await _context.SalesReps.ToListAsync(cancellationToken);
        var repByKey = new Dictionary<string, SalesRep>(StringComparer.OrdinalIgnoreCase);
        foreach (var rep in reps.Where(r => r.IsActive))
        {
            repByKey.TryAdd(rep.Id.ToString(), rep);
            repByKey.TryAdd(rep.Code, rep);
        }

        var rows = await _context.SalesOrders
            .Where(o => o.OrderDate >= periodStart && o.OrderDate < periodEndExclusive && o.SalesRepId != null)
            .SelectMany(o => o.Lines)
            .Where(l => l.ShippedQuantity > 0)
            .Select(l => new
            {
                RepKey = l.SalesOrder!.SalesRepId!,
                Revenue = (l.ShippedQuantity * l.UnitPrice) * (1m - (l.DiscountPercent / 100m)),
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            _logger.LogInformation("No shipped-line revenue found for commission week {PeriodStart:yyyy-MM-dd}.", periodStart);
            return new CommissionRunReport(null, periodStart, reportPeriodEnd, 0, 0m, 0m, SkippedDuplicate: false);
        }

        var run = new CommissionRun(
            $"COMM-{periodStart:yyyyMMdd}",
            periodStart,
            reportPeriodEnd);

        var unresolvedKeys = new List<string>();
        foreach (var group in rows.GroupBy(r => r.RepKey))
        {
            if (!repByKey.TryGetValue(group.Key, out var rep))
            {
                unresolvedKeys.Add(group.Key);
                continue;
            }

            run.AddLine(new CommissionRunLine(
                run.Id,
                rep.Id,
                rep.Code,
                periodStart,
                reportPeriodEnd,
                decimal.Round(group.Sum(r => r.Revenue), 2),
                rep.CommissionRate));
        }

        foreach (var key in unresolvedKeys)
        {
            _logger.LogWarning(
                "Commission revenue for sales rep reference '{RepKey}' could not be resolved to an active SalesRep; excluded from run {RunNumber}.",
                key,
                run.RunNumber);
        }

        _context.CommissionRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Commission run {RunNumber} created for week {PeriodStart:yyyy-MM-dd}: {RepCount} rep(s), revenue {TotalRevenue:C}, commission {TotalCommission:C}.",
            run.RunNumber,
            periodStart,
            run.Lines.Count,
            run.TotalRevenue,
            run.TotalCommission);

        return new CommissionRunReport(
            run.Id,
            periodStart,
            reportPeriodEnd,
            run.Lines.Count,
            run.TotalRevenue,
            run.TotalCommission,
            SkippedDuplicate: false);
    }

    internal static (DateTime PeriodStart, DateTime PeriodEndExclusive) PreviousIsoWeekUtc()
    {
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var currentWeekMonday = today.AddDays(-daysSinceMonday);
        var periodStart = currentWeekMonday.AddDays(-7);
        return (periodStart, currentWeekMonday);
    }
}
