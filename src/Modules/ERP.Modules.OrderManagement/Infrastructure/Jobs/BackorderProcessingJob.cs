// <copyright file="BackorderProcessingJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.OrderManagement.Infrastructure.Jobs;

public interface IBackorderProcessingJob
{
    Task<BackorderProcessingReport> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record BackorderProcessingReport(
    int BackorderedLinesExamined,
    int LinesReleased,
    int LinesStillShort);

/// <summary>
/// Finds confirmed / partially-shipped orders with unshipped backorder lines and,
/// for each distinct item+warehouse, asks Inventory (via the shared
/// <see cref="IInventoryAvailability"/> contract) whether stock is now available.
 /// Lines whose backordered quantity fits within the available stock are released
 /// to the pick queue (<see cref="SalesOrderLine.ReleaseBackorder"/>). Idempotent:
 /// already-released lines carry a <see cref="SalesOrderLine.BackorderReleasedOn"/>
 /// stamp and are skipped on the next run.
/// </summary>
public class BackorderProcessingJob : IBackorderProcessingJob
{
    private readonly OmDbContext _context;
    private readonly IInventoryAvailability _availability;
    private readonly ILogger<BackorderProcessingJob> _logger;

    public BackorderProcessingJob(
        OmDbContext context,
        IInventoryAvailability availability,
        ILogger<BackorderProcessingJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _availability = availability ?? throw new ArgumentNullException(nameof(availability));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BackorderProcessingReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _context.SalesOrders
            .Include(o => o.Lines)
            .Where(o => o.Status == SalesOrderStatus.Confirmed || o.Status == SalesOrderStatus.PartiallyShipped)
            .ToListAsync(cancellationToken);

        var candidates = orders
            .SelectMany(o => o.Lines.Select(l => new { Order = o, Line = l }))
            .Where(x => x.Line.BackorderedQuantity > 0
                && x.Line.BackorderReleasedOn is null
                && x.Line.WarehouseId.HasValue)
            .ToList();

        var examined = candidates.Count;
        var released = 0;
        var stillShort = 0;

        foreach (var group in candidates.GroupBy(
            x => new { ItemId = x.Line.ItemId, WarehouseId = x.Line.WarehouseId!.Value }))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var orderedByAge = group.OrderBy(x => x.Order.OrderDate).ThenBy(x => x.Line.LineNumber).ToList();
                var result = await _availability.CheckAsync(
                    group.Key.ItemId,
                    group.Key.WarehouseId,
                    orderedByAge.Sum(x => x.Line.BackorderedQuantity),
                    cancellationToken);

                var remaining = Math.Max(0m, result.Available);
                foreach (var candidate in orderedByAge)
                {
                    if (remaining >= candidate.Line.BackorderedQuantity)
                    {
                        candidate.Line.ReleaseBackorder();
                        remaining -= candidate.Line.BackorderedQuantity;
                        released++;
                        _logger.LogInformation(
                            "Backorder line {LineId} on order {OrderNumber} released ({Quantity} of item {ItemId}).",
                            candidate.Line.Id,
                            candidate.Order.OrderNumber,
                            candidate.Line.BackorderedQuantity,
                            group.Key.ItemId);
                    }
                    else
                    {
                        stillShort++;
                    }
                }
            }
            catch (Exception ex)
            {
                stillShort += group.Count();
                _logger.LogWarning(
                    ex,
                    "Availability check failed for item {ItemId} in warehouse {WarehouseId}; {Count} backorder line(s) left pending.",
                    group.Key.ItemId,
                    group.Key.WarehouseId,
                    group.Count());
            }
        }

        if (released > 0)
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Backorder processing completed: {Examined} line(s) examined, {Released} released, {Short} still short.",
            examined,
            released,
            stillShort);

        return new BackorderProcessingReport(examined, released, stillShort);
    }
}
