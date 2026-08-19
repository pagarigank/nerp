// <copyright file="RevaluationController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/gl/revaluation")]
public class RevaluationController : ControllerBase
{
    private readonly IRevaluationService _revaluationService;

    public RevaluationController(IRevaluationService revaluationService)
    {
        _revaluationService = revaluationService ?? throw new ArgumentNullException(nameof(revaluationService));
    }

    [HttpPost("preview")]
    public async Task<ActionResult<RevaluationPreviewDto>> Preview(
        [FromBody] RevaluationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var preview = await _revaluationService.PreviewRevaluationAsync(
            request.CompanyId,
            request.FiscalPeriodId,
            request.RevaluationDate,
            cancellationToken);

        return Ok(preview);
    }

    [HttpPost("execute")]
    public async Task<ActionResult<RevaluationResultDto>> Execute(
        [FromBody] RevaluationExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _revaluationService.RevalueAsync(
            request.CompanyId,
            request.FiscalPeriodId,
            request.RevaluationDate,
            request.RevaluationReason,
            cancellationToken);

        return Ok(new RevaluationResultDto(
            result.RevaluationBatch?.Id ?? Guid.Empty,
            result.RevaluationBatch?.BatchNumber ?? string.Empty,
            result.LinesRevalued,
            result.TotalGainLoss,
            result.RevaluationBatch?.CreatedOn ?? DateTimeOffset.UtcNow));
    }
}

public record RevaluationPreviewRequest(
    Guid CompanyId,
    Guid FiscalPeriodId,
    DateTimeOffset RevaluationDate);

public record RevaluationExecuteRequest(
    Guid CompanyId,
    Guid FiscalPeriodId,
    DateTimeOffset RevaluationDate,
    string RevaluationReason);

public record RevaluationResultDto(
    Guid BatchId,
    string BatchNumber,
    int LinesRevalued,
    decimal TotalGainLoss,
    DateTimeOffset CreatedOn);