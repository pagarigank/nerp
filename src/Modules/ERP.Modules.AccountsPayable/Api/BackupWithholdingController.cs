// <copyright file="BackupWithholdingController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/backup-withholding")]
public class BackupWithholdingController : ControllerBase
{
    private readonly IBackupWithholdingService _backupWithholdingService;

    public BackupWithholdingController(IBackupWithholdingService backupWithholdingService)
    {
        _backupWithholdingService = backupWithholdingService ?? throw new ArgumentNullException(nameof(backupWithholdingService));
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<BackupWithholdingResult>> Calculate(
        [FromBody] BackupWithholdingCalculateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _backupWithholdingService.CalculateWithholdingAsync(
            request.VendorId, request.PaymentAmount, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vendor/{vendorId:guid}")]
    public async Task<ActionResult<BackupWithholdingResult>> GetByVendor(
        Guid vendorId,
        [FromQuery] decimal paymentAmount,
        CancellationToken cancellationToken)
    {
        var result = await _backupWithholdingService.CalculateWithholdingAsync(vendorId, paymentAmount, cancellationToken);
        return Ok(result);
    }
}

public record BackupWithholdingCalculateRequest(
    Guid VendorId,
    decimal PaymentAmount);
