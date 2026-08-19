// <copyright file="ThreeWayMatchController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/three-way-match")]
public class ThreeWayMatchController : ControllerBase
{
    private readonly IThreeWayMatchService _threeWayMatchService;

    public ThreeWayMatchController(IThreeWayMatchService threeWayMatchService)
    {
        _threeWayMatchService = threeWayMatchService ?? throw new ArgumentNullException(nameof(threeWayMatchService));
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ThreeWayMatchResult>> Validate(
        [FromBody] ThreeWayMatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _threeWayMatchService.ValidateMatchAsync(request, cancellationToken);
        return Ok(result);
    }
}
