// <copyright file="ConsolidationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Api;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/consolidation")]
public class ConsolidationController : ControllerBase
{
    private readonly IConsolidationService _consolidationService;

    public ConsolidationController(IConsolidationService consolidationService)
    {
        _consolidationService = consolidationService ?? throw new ArgumentNullException(nameof(consolidationService));
    }

    [HttpPost("runs")]
    public async Task<ActionResult<ConsolidationRunDto>> CreateConsolidationRun(
        [FromBody] CreateConsolidationRunRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _consolidationService.CreateConsolidationRunAsync(
            request.ParentCompanyId,
            request.ConsolidationDate,
            request.FiscalYear,
            request.FiscalPeriod,
            request.Description,
            cancellationToken);

        return CreatedAtAction(nameof(GetConsolidationRun), new { id = run.Id }, MapToDto(run));
    }

    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<ConsolidationRunDto>> GetConsolidationRun(
        Guid id,
        CancellationToken cancellationToken)
    {
        var run = await _consolidationService.GetConsolidationRunAsync(id, cancellationToken);
        if (run == null)
            return NotFound();

        return Ok(MapToDto(run));
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<ConsolidationRunDto>>> GetConsolidationRuns(
        [FromQuery] Guid parentCompanyId,
        CancellationToken cancellationToken)
    {
        var runs = await _consolidationService.GetConsolidationRunsAsync(parentCompanyId, cancellationToken);
        return Ok(runs.Select(MapToDto).ToList());
    }

    [HttpPost("runs/{id:guid}/execute")]
    public async Task<ActionResult<ConsolidationRunDto>> ExecuteConsolidationRun(
        Guid id,
        CancellationToken cancellationToken)
    {
        var run = await _consolidationService.ExecuteConsolidationAsync(id, cancellationToken);
        return Ok(MapToDto(run));
    }

    [HttpPost("intercompany-mappings")]
    public async Task<ActionResult<IntercompanyMappingDto>> CreateIntercompanyMapping(
        [FromBody] CreateIntercompanyMappingRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await _consolidationService.CreateIntercompanyMappingAsync(
            request.FromCompanyId,
            request.ToCompanyId,
            request.FromAccountNumber,
            request.ToAccountNumber,
            request.Description,
            cancellationToken);

        return CreatedAtAction(nameof(GetIntercompanyMappings), new { mappingId = mapping.Id }, MapToDto(mapping));
    }

    [HttpGet("intercompany-mappings")]
    public async Task<ActionResult<IReadOnlyList<IntercompanyMappingDto>>> GetIntercompanyMappings(
        [FromQuery] Guid? fromCompanyId,
        [FromQuery] Guid? toCompanyId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var mappings = await _consolidationService.GetIntercompanyMappingsAsync(
            fromCompanyId, toCompanyId, isActive, cancellationToken);

        return Ok(mappings.Select(MapToDto).ToList());
    }

    [HttpPut("intercompany-mappings/{mappingId:guid}")]
    public async Task<ActionResult<IntercompanyMappingDto>> UpdateIntercompanyMapping(
        Guid mappingId,
        [FromBody] UpdateIntercompanyMappingRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await _consolidationService.UpdateIntercompanyMappingAsync(
            mappingId,
            request.FromAccountNumber,
            request.ToAccountNumber,
            request.Description,
            cancellationToken);

        return Ok(MapToDto(mapping));
    }

    [HttpDelete("intercompany-mappings/{mappingId:guid}")]
    public async Task<IActionResult> DeleteIntercompanyMapping(
        Guid mappingId,
        CancellationToken cancellationToken)
    {
        await _consolidationService.DeleteIntercompanyMappingAsync(mappingId, cancellationToken);
        return NoContent();
    }

    private static ConsolidationRunDto MapToDto(ConsolidationRun run)
    {
        return new ConsolidationRunDto(
            run.Id,
            run.ParentCompanyId,
            run.Description,
            run.ConsolidationDate,
            run.FiscalPeriodId,
            run.Status,
            run.ErrorMessage,
            run.CreatedOn,
            run.ModifiedOn);
    }

    private static IntercompanyMappingDto MapToDto(IntercompanyMapping mapping)
    {
        return new IntercompanyMappingDto(
            mapping.Id,
            mapping.FromCompanyId,
            mapping.ToCompanyId,
            mapping.FromAccountNumber,
            mapping.ToAccountNumber,
            mapping.Description,
            mapping.IsActive,
            mapping.CreatedOn,
            mapping.ModifiedOn);
    }
}

public record CreateConsolidationRunRequest(
    Guid ParentCompanyId,
    string Description,
    DateTimeOffset ConsolidationDate,
    int FiscalYear,
    int FiscalPeriod);

public record ConsolidationRunDto(
    Guid Id,
    Guid ParentCompanyId,
    string Description,
    DateTimeOffset ConsolidationDate,
    Guid FiscalPeriodId,
    ConsolidationRunStatus Status,
    string? ErrorMessage,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);

public record CreateIntercompanyMappingRequest(
    Guid FromCompanyId,
    Guid ToCompanyId,
    string FromAccountNumber,
    string ToAccountNumber,
    string Description);

public record UpdateIntercompanyMappingRequest(
    string FromAccountNumber,
    string ToAccountNumber,
    string Description);

public record IntercompanyMappingDto(
    Guid Id,
    Guid FromCompanyId,
    Guid ToCompanyId,
    string FromAccountNumber,
    string ToAccountNumber,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedOn,
    DateTimeOffset? ModifiedOn);