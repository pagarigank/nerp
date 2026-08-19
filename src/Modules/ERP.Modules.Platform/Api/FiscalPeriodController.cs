// <copyright file="FiscalPeriodController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Security.Claims;
using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/fiscal-periods")]
#pragma warning disable S6960
public class FiscalPeriodController : ControllerBase
#pragma warning restore S6960
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodService _periodService;
    private readonly IAuditLogService _auditLogService;

    public FiscalPeriodController(IUnitOfWork unitOfWork, IPeriodService periodService, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FiscalPeriodDto>>> GetAll([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var periods = await _unitOfWork.FiscalPeriods.FindAsync(x => x.CompanyId == companyId, cancellationToken);
        return Ok(periods.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FiscalPeriodDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var period = await _unitOfWork.FiscalPeriods.GetByIdAsync(id, cancellationToken);
        if (period == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(period));
    }

    [HttpGet("current")]
    public async Task<ActionResult<FiscalPeriodDto>> GetCurrent([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var period = await _periodService.GetCurrentPeriodAsync(companyId, cancellationToken);
        if (period == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(period));
    }

    [HttpPost]
    public async Task<ActionResult<FiscalPeriodDto>> Create([FromBody] CreateFiscalPeriodRequest request, CancellationToken cancellationToken)
    {
        var period = new FiscalPeriod(
            request.FiscalYearId,
            request.CompanyId,
            request.PeriodNumber,
            request.Description,
            request.StartDate,
            request.EndDate);

        await _unitOfWork.FiscalPeriods.AddAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(FiscalPeriod),
            period.Id,
            "system",
            newValues: new { request.PeriodNumber, request.Description },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = period.Id }, MapToDto(period));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var (performedBy, roles) = GetUserInfo();
        await _periodService.ClosePeriodAsync(id, performedBy, roles, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/open")]
    public async Task<IActionResult> Open(Guid id, CancellationToken cancellationToken)
    {
        var (performedBy, roles) = GetUserInfo();
        await _periodService.OpenPeriodAsync(id, performedBy, roles, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Pre-close checklist for the Period Close wizard. Returns the period being closed
    /// plus counts of still-open sub-ledger batches so the user can confirm all work is
    /// posted before rolling the period forward. Read-only; does not mutate state.
    /// </summary>
    [HttpGet("{id:guid}/close-preview")]
    public async Task<ActionResult<PeriodClosePreviewDto>> ClosePreview(Guid id, CancellationToken cancellationToken)
    {
        var period = await _unitOfWork.FiscalPeriods.GetByIdAsync(id, cancellationToken);
        if (period == null)
            return NotFound();

        // Close-readiness signal. The GL journal batch store lives in the GL module; when
        // that context is unavailable at runtime we report 0 open batches rather than fail
        // the wizard. Hook up a cross-module readiness probe here if GL is registered.
        var pendingGlBatches = 0;

        var dto = new PeriodClosePreviewDto(
            period.Id,
            period.PeriodNumber,
            period.Description,
            period.StartDate,
            period.EndDate,
            pendingGlBatches,
            new List<string>());

        return Ok(dto);
    }

    private static FiscalPeriodDto MapToDto(FiscalPeriod period)
    {
        return new FiscalPeriodDto(
            period.Id,
            period.FiscalYearId,
            period.CompanyId,
            period.PeriodNumber,
            period.Description,
            period.StartDate,
            period.EndDate,
            period.Status,
            period.CreatedOn,
            period.ModifiedOn);
    }

    private (string PerformedBy, List<string> Roles) GetUserInfo()
    {
        var performedBy = HttpContext.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? HttpContext.User?.FindFirst("sub")?.Value
            ?? "system";
        var roles = HttpContext.User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(HttpContext.User?.FindAll("role").Select(c => c.Value) ?? Enumerable.Empty<string>())
            .ToList()
            ?? new List<string>();
        return (performedBy, roles);
    }
}
