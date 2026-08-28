// <copyright file="DashboardWidgetsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/reporting/dashboard-widgets")]
public class DashboardWidgetsController : ControllerBase
{
    private readonly ReportingDbContext _context;

    public DashboardWidgetsController(ReportingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardWidgetDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? dashboardId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DashboardWidgets
            .Where(w => w.CompanyId == companyId && w.IsActive);

        if (!string.IsNullOrEmpty(dashboardId))
        {
            query = query.Where(w => w.DashboardId == dashboardId);
        }

        var widgets = await query
            .OrderBy(w => w.PositionY)
            .ThenBy(w => w.PositionX)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<DashboardWidgetDto>>.Success(
            widgets.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var widget = await _context.DashboardWidgets
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (widget == null)
        {
            return NotFound(ApiResponse<DashboardWidgetDto>.Failure(
                new[] { "Widget not found" }));
        }

        return Ok(ApiResponse<DashboardWidgetDto>.Success(MapToDto(widget)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> Create(
        [FromBody] CreateDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var widget = new DashboardWidget(
            request.CompanyId,
            request.DashboardId,
            request.Name,
            request.WidgetType,
            request.DataSourceType,
            request.DataSourceConfigJson,
            request.DisplayConfigJson,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height);

        _context.DashboardWidgets.Add(widget);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = widget.Id },
            ApiResponse<DashboardWidgetDto>.Success(MapToDto(widget)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DashboardWidgetDto>>> Update(
        Guid id,
        [FromBody] UpdateDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var widget = await _context.DashboardWidgets
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (widget == null)
        {
            return NotFound(ApiResponse<DashboardWidgetDto>.Failure(
                new[] { "Widget not found" }));
        }

        widget.Update(
            request.Name,
            request.WidgetType,
            request.DataSourceType,
            request.DataSourceConfigJson,
            request.DisplayConfigJson,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<DashboardWidgetDto>.Success(MapToDto(widget)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var widget = await _context.DashboardWidgets
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (widget == null)
        {
            return NotFound(ApiResponse<bool>.Failure(
                new[] { "Widget not found" }));
        }

        _context.DashboardWidgets.Remove(widget);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<bool>.Success(true));
    }

    private static DashboardWidgetDto MapToDto(DashboardWidget w) => new(
        w.Id,
        w.CompanyId,
        w.DashboardId,
        w.Name,
        w.WidgetType,
        w.DataSourceType,
        w.DataSourceConfigJson,
        w.DisplayConfigJson,
        w.PositionX,
        w.PositionY,
        w.Width,
        w.Height,
        w.RefreshIntervalSeconds,
        w.IsActive,
        w.CreatedOn,
        w.ModifiedOn);
}
