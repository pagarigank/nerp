// <copyright file="StatementLayoutsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/reporting/statement-layouts")]
public class StatementLayoutsController : ControllerBase
{
    private readonly ReportingDbContext _context;

    public StatementLayoutsController(ReportingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FinancialStatementLayoutDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? statementType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FinancialStatementLayouts
            .Where(l => l.CompanyId == companyId && l.IsActive);

        if (!string.IsNullOrEmpty(statementType))
        {
            query = query.Where(l => l.StatementType == statementType);
        }

        var layouts = await query
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<FinancialStatementLayoutDto>>.Success(
            layouts.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FinancialStatementLayoutDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var layout = await _context.FinancialStatementLayouts
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (layout == null)
        {
            return NotFound(ApiResponse<FinancialStatementLayoutDto>.Failure(
                new[] { "Layout not found" }));
        }

        return Ok(ApiResponse<FinancialStatementLayoutDto>.Success(MapToDto(layout)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FinancialStatementLayoutDto>>> Create(
        [FromBody] CreateStatementLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var layout = new FinancialStatementLayout(
            request.CompanyId,
            request.Name,
            request.StatementType,
            request.Description,
            request.RowDefinitionsJson,
            request.ColumnDefinitionsJson,
            request.TreeJson,
            request.SuppressZero,
            request.RoundToNearestDollar,
            1);

        _context.FinancialStatementLayouts.Add(layout);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = layout.Id },
            ApiResponse<FinancialStatementLayoutDto>.Success(MapToDto(layout)));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FinancialStatementLayoutDto>>> Update(
        Guid id,
        [FromBody] UpdateStatementLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var layout = await _context.FinancialStatementLayouts
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (layout == null)
        {
            return NotFound(ApiResponse<FinancialStatementLayoutDto>.Failure(
                new[] { "Layout not found" }));
        }

        layout.Update(
            request.Name,
            request.StatementType,
            request.Description ?? string.Empty,
            request.RowDefinitionsJson,
            request.ColumnDefinitionsJson,
            request.TreeJson,
            request.SuppressZero,
            request.RoundToNearestDollar);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<FinancialStatementLayoutDto>.Success(MapToDto(layout)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var layout = await _context.FinancialStatementLayouts
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (layout == null)
        {
            return NotFound(ApiResponse<bool>.Failure(
                new[] { "Layout not found" }));
        }

        _context.FinancialStatementLayouts.Remove(layout);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<bool>.Success(true));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<FinancialStatementLayoutDto>>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var layout = await _context.FinancialStatementLayouts
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (layout == null)
        {
            return NotFound(ApiResponse<FinancialStatementLayoutDto>.Failure(
                new[] { "Layout not found" }));
        }

        layout.Approve();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<FinancialStatementLayoutDto>.Success(MapToDto(layout)));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<FinancialStatementLayoutDto>>> Duplicate(
        Guid id,
        [FromBody] DuplicateLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var original = await _context.FinancialStatementLayouts
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (original == null)
        {
            return NotFound(ApiResponse<FinancialStatementLayoutDto>.Failure(
                new[] { "Layout not found" }));
        }

        var duplicate = new FinancialStatementLayout(
            original.CompanyId,
            request.NewName ?? $"{original.Name} (Copy)",
            original.StatementType,
            original.Description,
            original.RowDefinitionsJson,
            original.ColumnDefinitionsJson,
            original.TreeJson,
            original.SuppressZero,
            original.RoundToNearestDollar,
            1);

        _context.FinancialStatementLayouts.Add(duplicate);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = duplicate.Id },
            ApiResponse<FinancialStatementLayoutDto>.Success(MapToDto(duplicate)));
    }

    private static FinancialStatementLayoutDto MapToDto(FinancialStatementLayout l) => new(
        l.Id,
        l.CompanyId,
        l.Name,
        l.StatementType,
        l.Description,
        l.RowDefinitionsJson,
        l.ColumnDefinitionsJson,
        l.TreeJson,
        l.SuppressZero,
        l.RoundToNearestDollar,
        l.Version,
        l.IsApproved,
        l.IsActive,
        l.CreatedOn,
        l.ModifiedOn);
}

public record DuplicateLayoutRequest(string? NewName);
