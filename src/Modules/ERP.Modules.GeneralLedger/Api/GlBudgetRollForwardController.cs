// <copyright file="GlBudgetRollForwardController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/budgets")]
public class GlBudgetRollForwardController : ControllerBase
{
    private readonly IGlPeriodCloseService _service;

    public GlBudgetRollForwardController(IGlPeriodCloseService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpPost("{id:guid}/roll-forward")]
    public async Task<ActionResult<Guid>> RollForward(Guid id, [FromBody] RollForwardBudgetRequest request, CancellationToken cancellationToken)
    {
        var targetBudgetId = await _service.RollForwardBudgetAsync(id, request.TargetFiscalYearId, cancellationToken);
        return Ok(targetBudgetId);
    }

    [HttpPost("{id:guid}/transfer")]
    public async Task<ActionResult<Guid>> Transfer(Guid id, [FromBody] TransferBudgetRequest request, CancellationToken cancellationToken)
    {
        var budgetId = await _service.TransferBudgetAsync(
            id, request.AccountId, request.FromPeriodNumber, request.ToPeriodNumber, request.Amount, request.Reason, cancellationToken);
        return Ok(budgetId);
    }
}

public record RollForwardBudgetRequest(Guid TargetFiscalYearId);

public record TransferBudgetRequest(
    Guid AccountId, int FromPeriodNumber, int ToPeriodNumber, decimal Amount, string Reason);
