// <copyright file="JournalBatchController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IUnitOfWork = ERP.Modules.GeneralLedger.Infrastructure.IUnitOfWork;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/journal-batches")]
#pragma warning disable S6960
public class JournalBatchController : ControllerBase
#pragma warning restore S6960
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly GlDbContext _context;

    public JournalBatchController(IUnitOfWork unitOfWork, IJournalService journalService, GlDbContext context)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    [RequirePermission("gl.journal-batches.view")]
    public async Task<ActionResult<IReadOnlyList<JournalBatchDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var query = _context.JournalBatches.AsNoTracking();
        query = query.ApplyCompanyScope(HttpContext, b => b.CompanyId, companyId);

        var batches = await query.ToListAsync(cancellationToken);

        return Ok(batches.Select(b => MapBatchToDto(b)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JournalBatchDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _unitOfWork.JournalBatches.GetByIdAsync(id, cancellationToken);
        if (batch == null)
            return NotFound();

        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, lines.ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<JournalBatchDto>> Create([FromBody] CreateJournalBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await _journalService.CreateBatchAsync(
            request.CompanyId,
            request.BatchNumber,
            request.Description,
            request.PostingDate,
            request.FiscalPeriodId,
            cancellationToken);

        if (request.Lines?.Count > 0)
        {
            foreach (var line in request.Lines)
            {
                batch.AddLine(line.AccountId, line.Debit, line.Credit, line.Reference, line.SegmentsJson);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == batch.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, MapBatchToDto(batch, lines.ToList()));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<JournalBatchDto>> AddLine(Guid id, [FromBody] AddLineToBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await _unitOfWork.JournalBatches.GetByIdAsync(id, cancellationToken);
        if (batch == null)
            return NotFound();

        batch.AddLine(request.AccountId, request.Debit, request.Credit, request.Reference, request.SegmentsJson);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, lines.ToList()));
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        var batch = await _unitOfWork.JournalBatches.GetByIdAsync(id, cancellationToken);
        if (batch == null)
            return NotFound();

        batch.RemoveLine(lineId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<JournalBatchDto>> Release(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _unitOfWork.JournalBatches.GetByIdAsync(id, cancellationToken);
        if (batch == null)
            return NotFound();

        batch.Release();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, lines.ToList()));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<JournalBatchDto>> Post(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _journalService.PostBatchAsync(id, cancellationToken);
        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, lines.ToList()));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<JournalBatchDto>> Reverse(Guid id, [FromBody] ReverseBatchRequest request, CancellationToken cancellationToken)
    {
        var reversal = await _journalService.ReverseBatchAsync(id, request.Reason, cancellationToken);
        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == reversal.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = reversal.Id }, MapBatchToDto(reversal, lines.ToList()));
    }

    [HttpGet("next-number")]
    public async Task<ActionResult<string>> GetNextBatchNumber([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var count = await _unitOfWork.JournalBatches.CountAsync(x => x.CompanyId == companyId, cancellationToken);
        return Ok($"GL-{count + 1:D4}");
    }

    private static JournalBatchDto MapBatchToDto(JournalBatch batch, List<JournalEntryLine>? lines = null)
    {
        lines ??= [];

        var lineDtos = lines
            .Select(l => new JournalEntryLineDto(
                l.Id,
                l.AccountId,
                l.Debit,
                l.Credit,
                l.Reference,
                l.SegmentsJson))
            .ToList();

        return new JournalBatchDto(
            batch.Id,
            batch.CompanyId,
            batch.BatchNumber,
            batch.Description,
            batch.PostingDate,
            batch.FiscalPeriodId,
            batch.Status,
            lines.Sum(l => l.Debit),
            lines.Sum(l => l.Credit),
            lines.Count >= 2 && Math.Round(lines.Sum(l => l.Debit), 2) == Math.Round(lines.Sum(l => l.Credit), 2),
            batch.CreatedOn,
            batch.ModifiedOn,
            lineDtos);
    }
}
