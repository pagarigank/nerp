// <copyright file="SavedQueriesController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/reporting/saved-queries")]
public class SavedQueriesController : ControllerBase
{
    private readonly ReportingDbContext _context;

    public SavedQueriesController(ReportingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedQueryDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? module = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SavedQueries.Where(q => q.CompanyId == companyId);

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(q => q.Module == module);
        }

        if (activeOnly)
        {
            query = query.Where(q => q.IsActive);
        }

        var queries = await query
            .OrderByDescending(q => q.LastRunOn)
            .ThenBy(q => q.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<SavedQueryDto>>.Success(
            queries.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = await _context.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<SavedQueryDto>.Failure(
                new[] { "Saved query not found" }));
        }

        return Ok(ApiResponse<SavedQueryDto>.Success(MapToDto(query)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> Create(
        [FromBody] CreateSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = new SavedQuery(
            request.CompanyId,
            request.Name,
            request.Module,
            request.QueryType,
            request.EntityName,
            request.FilterJson,
            request.SortJson,
            request.ColumnSelectionJson,
            request.CreatedByUser);

        _context.SavedQueries.Add(query);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = query.Id },
            ApiResponse<SavedQueryDto>.Success(MapToDto(query)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> Update(
        Guid id,
        [FromBody] UpdateSavedQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = await _context.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<SavedQueryDto>.Failure(
                new[] { "Saved query not found" }));
        }

        query.Update(
            request.Name,
            request.Module,
            request.QueryType,
            request.EntityName,
            request.FilterJson,
            request.SortJson,
            request.ColumnSelectionJson);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<SavedQueryDto>.Success(MapToDto(query)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = await _context.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<bool>.Failure(
                new[] { "Saved query not found" }));
        }

        _context.SavedQueries.Remove(query);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("{id:guid}/run")]
    public async Task<ActionResult<ApiResponse<SavedQueryDto>>> Run(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = await _context.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (query == null)
        {
            return NotFound(ApiResponse<SavedQueryDto>.Failure(
                new[] { "Saved query not found" }));
        }

        query.RecordRun();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<SavedQueryDto>.Success(MapToDto(query)));
    }

    private static SavedQueryDto MapToDto(SavedQuery q) => new(
        q.Id,
        q.CompanyId,
        q.Name,
        q.Module,
        q.QueryType,
        q.EntityName,
        q.FilterJson,
        q.SortJson,
        q.ColumnSelectionJson,
        q.CreatedByUser,
        q.RunCount,
        q.LastRunOn,
        q.IsShared,
        q.IsActive,
        q.CreatedOn,
        q.ModifiedOn);
}
