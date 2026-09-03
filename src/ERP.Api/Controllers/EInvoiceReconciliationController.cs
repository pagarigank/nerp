// <copyright file="EInvoiceReconciliationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// API for e-Invoice reconciliation reports comparing ERP-posted documents
/// against MyInvois-confirmed documents.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/reconciliation")]
[ApiVersion("1.0")]
public class EInvoiceReconciliationController : ControllerBase
{
    private readonly IReconciliationReportService _service;
    private readonly ICurrentUserService _currentUser;

    public EInvoiceReconciliationController(
        IReconciliationReportService service,
        ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets the reconciliation report for a company within a date range.
    /// </summary>
    [HttpGet("report")]
    [RequirePermission("einvoice", "documents", "view")]
    [ProducesResponseType(typeof(ReconciliationReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReport(
        [FromQuery] Guid companyId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var report = await _service.GetReconciliationReportAsync(companyId, from, to, ct);
        return Ok(report);
    }

    /// <summary>
    /// Gets documents that are posted in ERP but not yet submitted to MyInvois.
    /// </summary>
    [HttpGet("unsubmitted")]
    [RequirePermission("einvoice", "documents", "view")]
    [ProducesResponseType(typeof(List<ReconciliationItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnsubmitted([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var items = await _service.GetUnsubmittedDocumentsAsync(companyId, ct);
        return Ok(items);
    }

    /// <summary>
    /// Gets documents that are submitted but not yet validated by MyInvois.
    /// </summary>
    [HttpGet("pending-validation")]
    [RequirePermission("einvoice", "documents", "view")]
    [ProducesResponseType(typeof(List<ReconciliationItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingValidation([FromQuery] Guid companyId, CancellationToken ct)
    {
        if (companyId == Guid.Empty)
            return BadRequest("CompanyId is required.");

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(companyId))
            return Forbid();

        var items = await _service.GetPendingValidationDocumentsAsync(companyId, ct);
        return Ok(items);
    }
}
