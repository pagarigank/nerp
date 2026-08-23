// <copyright file="BomJobsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.BillOfMaterials.Infrastructure.Jobs;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.BillOfMaterials.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/bom")]
public class BomJobsController : ControllerBase
{
    private readonly IBomValidationJob _validationJob;
    private readonly ICostRollupJob _costRollupJob;

    public BomJobsController(IBomValidationJob validationJob, ICostRollupJob costRollupJob)
    {
        _validationJob = validationJob;
        _costRollupJob = costRollupJob;
    }

    /// <summary>Runs the nightly BOM validation checks on demand.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation report (issues by BOM).</returns>
    [HttpPost("validation/run")]
    public async Task<ActionResult<ApiResponse<BomValidationReport>>> RunValidation(
        CancellationToken cancellationToken)
    {
        var report = await _validationJob.RunAsync(cancellationToken);
        return Ok(ApiResponse<BomValidationReport>.Success(report));
    }

    /// <summary>Runs the weekly multi-level standard-cost roll-up on demand.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The roll-up report (updated/unchanged counts and biggest deltas).</returns>
    [HttpPost("cost-rollup/run")]
    public async Task<ActionResult<ApiResponse<CostRollupReport>>> RunCostRollup(
        CancellationToken cancellationToken)
    {
        var report = await _costRollupJob.RunAsync(cancellationToken);
        return Ok(ApiResponse<CostRollupReport>.Success(report));
    }
}
