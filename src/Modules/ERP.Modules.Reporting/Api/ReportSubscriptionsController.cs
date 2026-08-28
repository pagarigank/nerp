// <copyright file="ReportSubscriptionsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reporting/subscriptions")]
public class ReportSubscriptionsController : ControllerBase
{
    private readonly ReportingDbContext _context;

    public ReportSubscriptionsController(ReportingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportSubscriptionDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReportSubscriptions
            .Where(s => s.CompanyId == companyId);

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var subscriptions = await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ReportSubscriptionDto>>.Success(
            subscriptions.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportSubscriptionDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sub = await _context.ReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sub == null)
        {
            return NotFound(ApiResponse<ReportSubscriptionDto>.Failure(
                new[] { "Subscription not found" }));
        }

        return Ok(ApiResponse<ReportSubscriptionDto>.Success(MapToDto(sub)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportSubscriptionDto>>> Create(
        [FromBody] CreateReportSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var sub = new ReportSubscription(
            request.CompanyId,
            request.ReportDefinitionId,
            request.Name,
            request.ParametersJson,
            request.ExportFormat,
            request.ScheduleType,
            request.ScheduleConfigJson,
            request.RecipientsJson);

        _context.ReportSubscriptions.Add(sub);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = sub.Id },
            ApiResponse<ReportSubscriptionDto>.Success(MapToDto(sub)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportSubscriptionDto>>> Update(
        Guid id,
        [FromBody] UpdateReportSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var sub = await _context.ReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sub == null)
        {
            return NotFound(ApiResponse<ReportSubscriptionDto>.Failure(
                new[] { "Subscription not found" }));
        }

        sub.Update(
            request.Name,
            request.ParametersJson,
            request.ExportFormat,
            request.ScheduleType,
            request.ScheduleConfigJson,
            request.RecipientsJson);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportSubscriptionDto>.Success(MapToDto(sub)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sub = await _context.ReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sub == null)
        {
            return NotFound(ApiResponse<bool>.Failure(
                new[] { "Subscription not found" }));
        }

        _context.ReportSubscriptions.Remove(sub);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<ReportSubscriptionDto>>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sub = await _context.ReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sub == null)
        {
            return NotFound(ApiResponse<ReportSubscriptionDto>.Failure(
                new[] { "Subscription not found" }));
        }

        sub.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportSubscriptionDto>.Success(MapToDto(sub)));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<ReportSubscriptionDto>>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var sub = await _context.ReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sub == null)
        {
            return NotFound(ApiResponse<ReportSubscriptionDto>.Failure(
                new[] { "Subscription not found" }));
        }

        sub.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportSubscriptionDto>.Success(MapToDto(sub)));
    }

    private static ReportSubscriptionDto MapToDto(ReportSubscription s) => new(
        s.Id,
        s.CompanyId,
        s.ReportDefinitionId,
        s.Name,
        s.ParametersJson,
        s.ExportFormat,
        s.ScheduleType,
        s.ScheduleConfigJson,
        s.RecipientsJson,
        s.LastRunOn,
        s.LastRunStatus,
        s.LastRunError,
        s.RunCount,
        s.Status,
        s.IsActive,
        s.CreatedOn,
        s.ModifiedOn);
}
