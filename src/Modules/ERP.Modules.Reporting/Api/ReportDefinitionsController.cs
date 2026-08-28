// <copyright file="ReportDefinitionsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/reporting/report-definitions")]
public class ReportDefinitionsController : ControllerBase
{
    private readonly ReportingDbContext _context;

    public ReportDefinitionsController(ReportingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportDefinitionDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? module = null,
        [FromQuery] string? category = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReportDefinitions
            .Where(r => r.CompanyId == companyId);

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(r => r.Module == module);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(r => r.Category == category);
        }

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        var reports = await query
            .OrderBy(r => r.Module)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ReportDefinitionDto>>.Success(
            reports.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null)
        {
            return NotFound(ApiResponse<ReportDefinitionDto>.Failure(
                new[] { "Report definition not found" }));
        }

        return Ok(ApiResponse<ReportDefinitionDto>.Success(MapToDto(report)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Create(
        [FromBody] CreateReportDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var report = new ReportDefinition(
            request.CompanyId,
            request.Name,
            request.Module,
            request.Category,
            request.Description,
            request.ReportType,
            request.DataSource,
            request.SqlQuery,
            request.ParametersJson,
            request.LayoutJson);

        _context.ReportDefinitions.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = report.Id },
            ApiResponse<ReportDefinitionDto>.Success(MapToDto(report)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Update(
        Guid id,
        [FromBody] UpdateReportDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null)
        {
            return NotFound(ApiResponse<ReportDefinitionDto>.Failure(
                new[] { "Report definition not found" }));
        }

        report.Update(
            request.Name,
            request.Module,
            request.Category,
            request.Description,
            request.ReportType,
            request.DataSource,
            request.SqlQuery,
            request.ParametersJson,
            request.LayoutJson);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportDefinitionDto>.Success(MapToDto(report)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null)
        {
            return NotFound(ApiResponse<bool>.Failure(
                new[] { "Report definition not found" }));
        }

        _context.ReportDefinitions.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null)
        {
            return NotFound(ApiResponse<ReportDefinitionDto>.Failure(
                new[] { "Report definition not found" }));
        }

        report.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportDefinitionDto>.Success(MapToDto(report)));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null)
        {
            return NotFound(ApiResponse<ReportDefinitionDto>.Failure(
                new[] { "Report definition not found" }));
        }

        report.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportDefinitionDto>.Success(MapToDto(report)));
    }

    [HttpPost("{id:guid}/share")]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Share(
        Guid id,
        [FromBody] ShareReportRequest request,
        CancellationToken cancellationToken)
    {
        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null)
        {
            return NotFound(ApiResponse<ReportDefinitionDto>.Failure(
                new[] { "Report definition not found" }));
        }

        report.SetShared(request.IsShared);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ReportDefinitionDto>.Success(MapToDto(report)));
    }

    private static ReportDefinitionDto MapToDto(ReportDefinition r) => new(
        r.Id,
        r.CompanyId,
        r.Name,
        r.Module,
        r.Category,
        r.Description,
        r.ReportType,
        r.DataSource,
        r.SqlQuery,
        r.ParametersJson,
        r.LayoutJson,
        r.IsShared,
        r.IsActive,
        r.CreatedOn,
        r.ModifiedOn);
}
