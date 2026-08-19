// <copyright file="FinanceChargesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/finance-charges")]
public class FinanceChargesController : ControllerBase
{
    private readonly IFinanceChargeService _financeChargeService;

    public FinanceChargesController(IFinanceChargeService financeChargeService)
    {
        _financeChargeService = financeChargeService ?? throw new ArgumentNullException(nameof(financeChargeService));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult> CalculateAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken,
        [FromQuery] decimal annualRate = 18.0m)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var charges = await _financeChargeService.CalculateChargesAsync(companyId, annualRate, asOfDate, cancellationToken);
        return Ok(new { count = charges.Count, asOfDate, annualRate });
    }
}
