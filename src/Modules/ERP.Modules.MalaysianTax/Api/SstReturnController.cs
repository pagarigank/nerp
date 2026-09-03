// <copyright file="SstReturnController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.Platform.Api.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// CRUD and computation endpoints for SST-02/SST-02A returns.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SstReturnController : ControllerBase
{
    private readonly ISstReturnService _sstReturnService;
    private readonly ILogger<SstReturnController> _logger;

    public SstReturnController(
        ISstReturnService sstReturnService,
        ILogger<SstReturnController> logger)
    {
        _sstReturnService = sstReturnService;
        _logger = logger;
    }

    /// <summary>
    /// Get all SST returns for the current company.
    /// </summary>
    [HttpGet]
    [RequirePermission("tax", "sst-returns", "view")]
    [ProducesResponseType(typeof(IReadOnlyList<SstReturn>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companyId = GetCompanyId();
        if (companyId == Guid.Empty)
        {
            return BadRequest("Company context is required.");
        }

        var returns = await _sstReturnService.GetReturnsAsync(companyId, ct);
        return Ok(returns);
    }

    /// <summary>
    /// Get a single SST return by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SstReturn), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sstReturn = await _sstReturnService.GetReturnByIdAsync(id, ct);
        if (sstReturn == null)
        {
            return NotFound("SST return not found.");
        }

        return Ok(sstReturn);
    }

    /// <summary>
    /// Compute or recompute an SST return for a given period.
    /// </summary>
    [HttpPost("compute")]
    [RequirePermission("tax", "sst-returns", "create")]
    [ProducesResponseType(typeof(SstReturn), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compute(
        [FromBody] ComputeReturnRequest request,
        CancellationToken ct)
    {
        var companyId = GetCompanyId();
        if (companyId == Guid.Empty)
        {
            return BadRequest("Company context is required.");
        }

        if (string.IsNullOrEmpty(request.ReturnType))
        {
            return BadRequest("ReturnType is required (SST-02 or SST-02A).");
        }

        try
        {
            var sstReturn = await _sstReturnService.ComputeReturnAsync(
                companyId, request.PeriodStart, request.ReturnType, ct);
            return Ok(sstReturn);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Mark a prepared return as filed with LHDN.
    /// </summary>
    [HttpPost("{id:guid}/file")]
    [RequirePermission("tax", "sst-returns", "edit")]
    [ProducesResponseType(typeof(SstReturn), StatusCodes.Status200OK)]
    public async Task<IActionResult> File(
        Guid id,
        [FromBody] FileReturnRequest request,
        CancellationToken ct)
    {
        try
        {
            var filed = await _sstReturnService.FileReturnAsync(
                id, request.LhdnReference, ct);
            return Ok(filed);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("SST return not found.");
        }
    }

    /// <summary>
    /// Amend a filed return — marks the original as Amended and creates a new draft.
    /// </summary>
    [HttpPost("{id:guid}/amend")]
    [RequirePermission("tax", "sst-returns", "edit")]
    [ProducesResponseType(typeof(SstReturn), StatusCodes.Status200OK)]
    public async Task<IActionResult> Amend(Guid id, CancellationToken ct)
    {
        try
        {
            var amended = await _sstReturnService.AmendReturnAsync(id, ct);
            return Ok(amended);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("SST return not found.");
        }
    }

    /// <summary>
    /// Extract CompanyId from the HTTP context claims.
    /// </summary>
    private Guid GetCompanyId()
    {
        var claim = User?.FindFirst("CompanyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : Guid.Empty;
    }
}
