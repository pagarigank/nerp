// <copyright file="GlPeriodCloseController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/period-close")]
public class GlPeriodCloseController : ControllerBase
{
    private readonly IGlPeriodCloseService _service;

    public GlPeriodCloseController(IGlPeriodCloseService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpPost("year-end")]
    public async Task<ActionResult<YearEndCloseRunDto>> CloseYearEnd(
        [FromBody] CloseYearEndRequest request, CancellationToken cancellationToken)
    {
        var run = await _service.CloseYearEndAsync(
            request.CompanyId, request.FiscalYearId, request.RetainedEarningsAccountId, request.ClosedBy, cancellationToken);
        return Ok(MapToDto(run));
    }

    [HttpGet("suspense")]
    public async Task<ActionResult<IReadOnlyList<PostingSuspenseItemDto>>> GetSuspense(
        [FromQuery] Guid companyId, [FromQuery] SuspenseStatus? status, CancellationToken cancellationToken)
    {
        var items = await _service.GetSuspenseItemsAsync(companyId, status, cancellationToken);
        return Ok(items.Select(MapToDto).ToList());
    }

    [HttpPost("suspense/{id:guid}/resolve")]
    public async Task<ActionResult<Guid>> ResolveSuspense(
        Guid id, [FromBody] ResolveSuspenseRequest request, CancellationToken cancellationToken)
    {
        var batchId = await _service.ResolveSuspenseAsync(id, request.AccountId, request.Debit, request.Credit, cancellationToken);
        return Ok(batchId);
    }

    [HttpPost("suspense/{id:guid}/discard")]
    public async Task<IActionResult> DiscardSuspense(
        Guid id, [FromBody] DiscardSuspenseRequest request, CancellationToken cancellationToken)
    {
        await _service.DiscardSuspenseAsync(id, request.Note, cancellationToken);
        return NoContent();
    }

    [HttpPost("intercompany-due-to-from")]
    public async Task<IActionResult> PostIntercompanyDueToFrom(
        [FromBody] IntercompanyDueToFromRequest request, CancellationToken cancellationToken)
    {
        await _service.PostIntercompanyDueToFromAsync(
            request.CompanyId,
            request.FromCompanyId,
            request.ToCompanyId,
            request.Amount,
            request.DueFromAccountId,
            request.DueToAccountId,
            request.OffsetAccountId,
            request.Reason,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("pre-posting")]
    public async Task<ActionResult<IReadOnlyList<PrePostingEditLine>>> GetPrePosting(
        [FromQuery] Guid companyId, [FromQuery] Guid fiscalPeriodId, CancellationToken cancellationToken)
    {
        var lines = await _service.GetPrePostingEditListAsync(companyId, fiscalPeriodId, cancellationToken);
        return Ok(lines);
    }

    [HttpGet("checklist")]
    public async Task<ActionResult<IReadOnlyList<PeriodEndChecklistItem>>> GetChecklist(
        [FromQuery] Guid companyId, [FromQuery] Guid fiscalPeriodId, CancellationToken cancellationToken)
    {
        var items = await _service.GetPeriodEndChecklistAsync(companyId, fiscalPeriodId, cancellationToken);
        return Ok(items);
    }

    private static YearEndCloseRunDto MapToDto(YearEndCloseRun run) => new(
        run.Id,
        run.CompanyId,
        run.FiscalYearId,
        run.RetainedEarningsAccountId,
        run.ClosedOn,
        run.ClosedBy,
        run.TotalRevenue,
        run.TotalExpense,
        run.RetainedEarningsAmount,
        run.Status);

    private static PostingSuspenseItemDto MapToDto(PostingSuspenseItem s) => new(
        s.Id,
        s.CompanyId,
        s.SourceModule,
        s.SourceReference,
        s.AccountId,
        s.Debit,
        s.Credit,
        s.CurrencyId,
        s.ReasonCode,
        s.ErrorMessage,
        s.Status,
        s.ResolvedBatchId);
}

public record CloseYearEndRequest(
    Guid CompanyId, Guid FiscalYearId, Guid RetainedEarningsAccountId, string ClosedBy);

public record ResolveSuspenseRequest(Guid AccountId, decimal Debit, decimal Credit);

public record DiscardSuspenseRequest(string? Note);

public record IntercompanyDueToFromRequest(
    Guid CompanyId, Guid FromCompanyId, Guid ToCompanyId, decimal Amount,
    Guid DueFromAccountId, Guid DueToAccountId, Guid OffsetAccountId, string Reason);

public record YearEndCloseRunDto(
    Guid Id, Guid CompanyId, Guid FiscalYearId, Guid RetainedEarningsAccountId,
    DateTimeOffset ClosedOn, string ClosedBy, decimal TotalRevenue, decimal TotalExpense,
    decimal RetainedEarningsAmount, YearEndCloseStatus Status);

public record PostingSuspenseItemDto(
    Guid Id, Guid CompanyId, string SourceModule, string SourceReference, Guid? AccountId,
    decimal Debit, decimal Credit, Guid? CurrencyId, string ReasonCode, string ErrorMessage,
    SuspenseStatus Status, Guid? ResolvedBatchId);
