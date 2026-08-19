// <copyright file="Form1099Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/1099")]
public class Form1099Controller : ControllerBase
{
    private readonly IForm1099Service _form1099Service;

    public Form1099Controller(IForm1099Service form1099Service)
    {
        _form1099Service = form1099Service ?? throw new ArgumentNullException(nameof(form1099Service));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Form1099SummaryResult>> GetSummary(
        [FromQuery] Guid companyId,
        [FromQuery] int taxYear,
        CancellationToken cancellationToken)
    {
        var result = await _form1099Service.Get1099SummaryAsync(companyId, taxYear, cancellationToken);
        return Ok(result);
    }

    [HttpGet("efile")]
    public async Task<ActionResult<string>> GetEfile(
        [FromQuery] Guid companyId,
        [FromQuery] int taxYear,
        CancellationToken cancellationToken)
    {
        var content = await _form1099Service.GenerateEfileContentAsync(companyId, taxYear, cancellationToken);
        return Content(content, "text/csv", System.Text.Encoding.UTF8);
    }
}
