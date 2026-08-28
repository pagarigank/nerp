// <copyright file="ReportParameterSetsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Api;

[ApiController]
[Route("api/v1/reporting/parameter-sets")]
public class ReportParameterSetsController : ControllerBase
{
    private readonly ReportingDbContext _db;

    public ReportParameterSetsController(ReportingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid reportDefinitionId)
    {
        var sets = await _db.ReportParameterSets
            .Where(x => x.ReportDefinitionId == reportDefinitionId && !x.DeletedOn.HasValue)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(sets));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var set = await _db.ReportParameterSets.FindAsync(id);
        if (set == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Parameter set not found" }));

        return Ok(ApiResponse<object>.Success(set));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReportParameterSetCreateDto dto)
    {
        if (dto.IsDefault)
        {
            var existingDefaults = await _db.ReportParameterSets
                .Where(x => x.ReportDefinitionId == dto.ReportDefinitionId && x.IsDefault && !x.DeletedOn.HasValue)
                .ToListAsync();
            foreach (var existing in existingDefaults)
            {
                existing.SetDefault(false);
            }
        }

        var set = new ReportParameterSet(
            dto.CompanyId,
            dto.ReportDefinitionId,
            dto.Name,
            dto.ParametersJson,
            dto.IsDefault,
            dto.Description);

        _db.ReportParameterSets.Add(set);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = set.Id },
            ApiResponse<object>.Success(set));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReportParameterSetUpdateDto dto)
    {
        var set = await _db.ReportParameterSets.FindAsync(id);
        if (set == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Parameter set not found" }));

        if (dto.IsDefault && !set.IsDefault)
        {
            var existingDefaults = await _db.ReportParameterSets
                .Where(x => x.ReportDefinitionId == set.ReportDefinitionId
                    && x.IsDefault
                    && x.Id != id
                    && !x.DeletedOn.HasValue)
                .ToListAsync();
            foreach (var existing in existingDefaults)
            {
                existing.SetDefault(false);
            }
        }

        set.Update(dto.Name, dto.ParametersJson, dto.IsDefault, dto.Description);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(set));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var set = await _db.ReportParameterSets.FindAsync(id);
        if (set == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Parameter set not found" }));

        set.MarkDeleted("system");
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(set));
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        var set = await _db.ReportParameterSets.FindAsync(id);
        if (set == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Parameter set not found" }));

        var existingDefaults = await _db.ReportParameterSets
            .Where(x => x.ReportDefinitionId == set.ReportDefinitionId
                && x.IsDefault
                && x.Id != id
                && !x.DeletedOn.HasValue)
            .ToListAsync();
        foreach (var existing in existingDefaults)
        {
            existing.SetDefault(false);
        }

        set.SetDefault(true);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(set));
    }

    [HttpPost("{id:guid}/run")]
    public async Task<IActionResult> Run(Guid id)
    {
        var set = await _db.ReportParameterSets.FindAsync(id);
        if (set == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Parameter set not found" }));

        set.IncrementRunCount();
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            set.Id,
            set.Name,
            set.ParametersJson,
            set.RunCount
        }));
    }
}

public class ReportParameterSetCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid ReportDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? Description { get; set; }
}

public class ReportParameterSetUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? Description { get; set; }
}
