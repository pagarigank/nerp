// <copyright file="ReconciliationsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cash/reconciliations")]
public class ReconciliationsController : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly IReconciliationService _service;

    public ReconciliationsController(CashDbContext context, IReconciliationService service)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReconciliationSessionResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.ReconciliationSessions.AsNoTracking();

        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        var sessions = await query
            .OrderByDescending(s => s.StatementDate)
            .Select(s => Map(s))
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReconciliationSessionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == id && !s.DeletedOn.HasValue, cancellationToken);

        if (session == null)
            return NotFound();

        return Ok(Map(session));
    }

    [HttpGet("{id:guid}/lines")]
    public async Task<ActionResult<IReadOnlyList<BankStatementLineResponse>>> GetLinesAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == id && !s.DeletedOn.HasValue, cancellationToken);

        if (session == null)
            return NotFound();

        var lines = await _context.BankStatementLines
            .Where(l => l.BankStatementId == session.BankStatementId)
            .OrderBy(l => l.TransactionDate)
            .Select(l => new BankStatementLineResponse(
                l.Id,
                l.TransactionDate,
                l.Amount,
                l.Description,
                l.ReferenceNumber,
                l.CheckNumber,
                l.Balance,
                l.Status.ToString(),
                l.MatchedTransactionId,
                l.MatchedSource != null ? l.MatchedSource.ToString() : null))
            .ToListAsync(cancellationToken);

        return Ok(lines);
    }

    [HttpPost("statement/{statementId:guid}")]
    public async Task<ActionResult<CreateReconciliationSessionResponse>> CreateSessionAsync(
        Guid statementId,
        CreateReconciliationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _service.CreateSessionAsync(statementId, request.SessionNumber, request.CreatedBy, cancellationToken);

        return CreatedAtAction("GetById", new { id = session.Id }, new CreateReconciliationSessionResponse(
            session.Id,
            session.SessionNumber,
            session.BeginningBalance,
            session.EndingBalance));
    }

    [HttpPost("{id:guid}/auto-match")]
    public async Task<ActionResult<IReadOnlyList<AutoMatchLineResponse>>> RunAutoMatchAsync(Guid id, CancellationToken cancellationToken)
    {
        var results = await _service.RunAutoMatchAsync(id, cancellationToken);

        var response = results.Select(r =>
        {
            var candidate = r.Candidate == null
                ? null
                : new AutoMatchCandidateResponse(
                    r.Candidate.Id,
                    r.Candidate.Source.ToString(),
                    r.Candidate.Reference,
                    r.Candidate.Amount,
                    r.Candidate.Date,
                    r.Candidate.CheckNumber,
                    r.Candidate.Description);

            return new AutoMatchLineResponse(r.StatementLineId, r.StatementAmount, candidate, r.Score, r.Confidence.ToString());
        }).ToList();

        return Ok(response);
    }

    [HttpPost("{id:guid}/lines/match")]
    public async Task<ActionResult<BankStatementLineResponse>> MarkLineMatchedAsync(
        Guid id,
        MarkLineMatchedRequest request,
        CancellationToken cancellationToken)
    {
        var line = await _service.MarkLineMatchedAsync(
            id,
            request.StatementLineId,
            request.TransactionId,
            (BankMatchSource)request.Source,
            request.ClearedBy ?? "admin",
            cancellationToken);

        return Ok(MapLine(line));
    }

    [HttpPost("{id:guid}/lines/clear")]
    public async Task<ActionResult<BankStatementLineResponse>> MarkLineClearedAsync(
        Guid id,
        MarkLineClearedRequest request,
        CancellationToken cancellationToken)
    {
        var line = await _service.MarkLineClearedAsync(id, request.StatementLineId, request.ClearedBy ?? "admin", cancellationToken);
        return Ok(MapLine(line));
    }

    [HttpPost("{id:guid}/lines/unmatch")]
    public async Task<ActionResult<BankStatementLineResponse>> MarkLineUnmatchedAsync(
        Guid id,
        MarkLineUnmatchedRequest request,
        CancellationToken cancellationToken)
    {
        var line = await _service.MarkLineUnmatchedAsync(id, request.StatementLineId, request.ClearedBy ?? "admin", cancellationToken);
        return Ok(MapLine(line));
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<ActionResult<ReconciliationSessionResponse>> LockAsync(
        Guid id,
        LockReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _service.LockSessionAsync(
            id,
            request.VarianceGlAccountId,
            request.Tolerance,
            request.LockedBy ?? "admin",
            cancellationToken);

        return Ok(Map(session));
    }

    private static ReconciliationSessionResponse Map(ReconciliationSession session) => new(
        session.Id,
        session.CompanyId,
        session.BankAccountId,
        session.BankStatementId,
        session.SessionNumber,
        session.StatementDate,
        session.BeginningBalance,
        session.EndingBalance,
        session.Variance,
        session.GlJournalBatchId,
        session.Status.ToString());

    private static BankStatementLineResponse MapLine(BankStatementLine line) => new(
        line.Id,
        line.TransactionDate,
        line.Amount,
        line.Description,
        line.ReferenceNumber,
        line.CheckNumber,
        line.Balance,
        line.Status.ToString(),
        line.MatchedTransactionId,
        line.MatchedSource?.ToString());
}
