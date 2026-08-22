// <copyright file="RecurringTemplateController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Api;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IUnitOfWork = ERP.Modules.GeneralLedger.Infrastructure.IUnitOfWork;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/recurring-templates")]
#pragma warning disable S6960
public class RecurringTemplateController : ControllerBase
#pragma warning restore S6960
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly GlDbContext _context;

    public RecurringTemplateController(IUnitOfWork unitOfWork, IJournalService journalService, GlDbContext context)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecurringTemplateDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var query = _context.RecurringTemplates.AsNoTracking();
        query = query.ApplyCompanyScope(HttpContext, t => t.CompanyId, companyId);

        var templates = await query.ToListAsync(cancellationToken);

        return Ok(templates.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecurringTemplateDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.RecurringTemplates.GetByIdAsync(id, cancellationToken);
        if (template == null)
            return NotFound();

        return Ok(MapToDto(template));
    }

    [HttpPost]
    public async Task<ActionResult<RecurringTemplateDto>> Create([FromBody] CreateRecurringTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await _journalService.CreateRecurringTemplateAsync(
            request.CompanyId,
            request.Name,
            request.Description,
            request.Frequency,
            request.NextRunDate,
            request.IsActive,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = template.Id }, MapToDto(template));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RecurringTemplateDto>> Update(Guid id, [FromBody] UpdateRecurringTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await _journalService.UpdateRecurringTemplateAsync(
            id,
            request.Name,
            request.Description,
            request.Frequency,
            request.NextRunDate,
            request.IsActive,
            cancellationToken);

        return Ok(MapToDto(template));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<RecurringTemplateDto>> AddLine(Guid id, [FromBody] AddRecurringTemplateLineRequest request, CancellationToken cancellationToken)
    {
        var template = await _journalService.AddRecurringTemplateLineAsync(
            id,
            request.AccountId,
            request.FixedDebit,
            request.FixedCredit,
            request.VariablePct,
            request.Reference,
            cancellationToken);

        return Ok(MapToDto(template));
    }

    [HttpPost("{id:guid}/generate")]
    public async Task<ActionResult<JournalBatchDto>> GenerateFromTemplate(Guid id, [FromBody] GenerateFromRecurringRequest request, CancellationToken cancellationToken)
    {
        var batch = await _journalService.GenerateFromRecurringAsync(
            id,
            request.BatchNumber,
            request.FiscalPeriodId,
            request.PostingDate,
            cancellationToken);

        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == batch.Id, cancellationToken);
        return CreatedAtAction("GetById", "JournalBatch", new { id = batch.Id }, MapBatchToDto(batch, lines.ToList()));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.RecurringTemplates.GetByIdAsync(id, cancellationToken);
        if (template == null)
            return NotFound();

        template.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.RecurringTemplates.GetByIdAsync(id, cancellationToken);
        if (template == null)
            return NotFound();

        template.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static RecurringTemplateDto MapToDto(RecurringTemplate template)
    {
        var lines = template.Lines
            .Select(l => new RecurringTemplateLineDto(
                l.Id,
                l.AccountId,
                l.FixedDebit,
                l.FixedCredit,
                l.VariablePct,
                l.Reference))
            .ToList();

        return new RecurringTemplateDto(
            template.Id,
            template.CompanyId,
            template.Name,
            template.Description,
            template.Frequency,
            template.NextRunDate,
            template.LastRunDate,
            template.IsActive,
            lines);
    }

    private static JournalBatchDto MapBatchToDto(JournalBatch batch, List<JournalEntryLine> lines)
    {
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
            batch.Lines.Sum(l => l.Debit),
            batch.Lines.Sum(l => l.Credit),
            batch.Lines.Count >= 2 && Math.Round(batch.Lines.Sum(l => l.Debit), 2) == Math.Round(batch.Lines.Sum(l => l.Credit), 2),
            batch.CreatedOn,
            batch.ModifiedOn,
            lineDtos);
    }
}

public record UpdateRecurringTemplateRequest(
    string Name,
    string Description,
    RecurringFrequency Frequency,
    DateTimeOffset NextRunDate,
    bool IsActive);