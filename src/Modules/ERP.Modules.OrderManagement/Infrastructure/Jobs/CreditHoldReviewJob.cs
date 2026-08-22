// <copyright file="CreditHoldReviewJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.OrderManagement.Infrastructure.Jobs;

public interface ICreditHoldReviewJob
{
    Task<CreditHoldReviewReport> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record CreditHoldOrderRow(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string? Reason,
    DateTime OrderDate,
    int DaysOnHold);

public sealed record CreditHoldReviewReport(int HeldOrderCount, IReadOnlyList<CreditHoldOrderRow> Orders);

/// <summary>
/// Daily credit-hold review: lists every sales order currently on credit hold
/// with its aging (days since order date) so A/R can follow up before the
/// backlog ages out of acceptable terms.
/// </summary>
public class CreditHoldReviewJob : ICreditHoldReviewJob
{
    private readonly OmDbContext _context;
    private readonly ILogger<CreditHoldReviewJob> _logger;

    public CreditHoldReviewJob(OmDbContext context, ILogger<CreditHoldReviewJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreditHoldReviewReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var held = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.IsOnCreditHold)
            .OrderBy(o => o.OrderDate)
            .Select(o => new { o.Id, o.OrderNumber, o.CustomerId, o.CreditHoldReason, o.OrderDate })
            .ToListAsync(cancellationToken);

        var rows = held
            .Select(o => new CreditHoldOrderRow(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.CreditHoldReason,
                o.OrderDate,
                (today - o.OrderDate.Date).Days))
            .ToList();

        if (rows.Count > 0)
        {
            var oldest = rows[0];
            _logger.LogWarning(
                "Credit-hold review: {Count} sales order(s) on credit hold; oldest {OldestDays} day(s) ({OldestOrder}).",
                rows.Count,
                oldest.DaysOnHold,
                oldest.OrderNumber);

            foreach (var row in rows.Take(10))
            {
                _logger.LogWarning(
                    "Credit hold: {OrderNumber}, customer {CustomerId}, {Days} day(s) on hold, reason: {Reason}",
                    row.OrderNumber,
                    row.CustomerId,
                    row.DaysOnHold,
                    row.Reason ?? "(none)");
            }
        }
        else
        {
            _logger.LogInformation("Credit-hold review: no sales orders on credit hold.");
        }

        return new CreditHoldReviewReport(rows.Count, rows);
    }
}
