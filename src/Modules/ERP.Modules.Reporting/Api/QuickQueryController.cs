// <copyright file="QuickQueryController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/reporting/quick-queries")]
public class QuickQueryController : ControllerBase
{
    private readonly ReportingDbContext _context;

    public QuickQueryController(ReportingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QuickQueryDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? entityName = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.QuickQueries
            .Where(q => q.CompanyId == companyId);

        if (!string.IsNullOrEmpty(entityName))
        {
            query = query.Where(q => q.EntityName == entityName);
        }

        if (activeOnly)
        {
            query = query.Where(q => q.IsActive);
        }

        var queries = await query
            .OrderByDescending(q => q.LastRunOn)
            .ThenBy(q => q.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<QuickQueryDto>>.Success(
            queries.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuickQueryDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = await _context.QuickQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<QuickQueryDto>.Failure(
                new[] { "Quick query not found" }));
        }

        return Ok(ApiResponse<QuickQueryDto>.Success(MapToDto(query)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuickQueryDto>>> Create(
        [FromBody] CreateQuickQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = new QuickQuery(
            request.CompanyId,
            request.Name,
            request.EntityName,
            request.FilterJson,
            request.SortJson,
            request.ColumnSelectionJson,
            request.IncludeArchived,
            request.CreatedByUser);

        _context.QuickQueries.Add(query);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = query.Id },
            ApiResponse<QuickQueryDto>.Success(MapToDto(query)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<QuickQueryDto>>> Update(
        Guid id,
        [FromBody] UpdateQuickQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = await _context.QuickQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<QuickQueryDto>.Failure(
                new[] { "Quick query not found" }));
        }

        query.Update(
            request.Name,
            request.EntityName,
            request.FilterJson,
            request.SortJson,
            request.ColumnSelectionJson,
            request.IncludeArchived);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<QuickQueryDto>.Success(MapToDto(query)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = await _context.QuickQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<bool>.Failure(
                new[] { "Quick query not found" }));
        }

        _context.QuickQueries.Remove(query);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("{id:guid}/run")]
    public async Task<ActionResult<ApiResponse<QuickQueryDto>>> Run(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = await _context.QuickQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<QuickQueryDto>.Failure(
                new[] { "Quick query not found" }));
        }

        query.RecordRun();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<QuickQueryDto>.Success(MapToDto(query)));
    }

    private static QuickQueryDto MapToDto(QuickQuery q) => new(
        q.Id,
        q.CompanyId,
        q.Name,
        q.EntityName,
        q.FilterJson,
        q.SortJson,
        q.ColumnSelectionJson,
        q.IncludeArchived,
        q.CreatedByUser,
        q.RunCount,
        q.LastRunOn,
        q.IsShared,
        q.IsActive,
        q.CreatedOn,
        q.ModifiedOn);
}
