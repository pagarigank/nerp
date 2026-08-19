// <copyright file="CreditLimitController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Core.Common;
using ERP.Modules.AccountsReceivable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/credit-limit")]
public class CreditLimitController : ControllerBase
{
    private readonly ICreditLimitCheckService _creditLimitService;

    public CreditLimitController(ICreditLimitCheckService creditLimitService)
    {
        _creditLimitService = creditLimitService ?? throw new ArgumentNullException(nameof(creditLimitService));
    }

    [HttpGet("check/{customerId:guid}")]
    public async Task<ActionResult<CreditLimitCheckResult>> CheckAsync(
        Guid customerId,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken)
    {
        var result = await _creditLimitService.CheckAsync(customerId, amount, cancellationToken);
        return Ok(result);
    }
}
