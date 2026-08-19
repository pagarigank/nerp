// <copyright file="AllocationRuleController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Api;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/allocation-rules")]
public class AllocationRuleController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;

    public AllocationRuleController(IUnitOfWork unitOfWork, IJournalService journalService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AllocationRuleDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var rules = companyId.HasValue
            ? await _unitOfWork.AllocationRules.FindAsync(x => x.CompanyId == companyId.Value, cancellationToken)
            : await _unitOfWork.AllocationRules.GetAllAsync(cancellationToken);

        return Ok(rules.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AllocationRuleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.AllocationRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        return Ok(MapToDto(rule));
    }

    [HttpPost]
    public async Task<ActionResult<AllocationRuleDto>> Create([FromBody] CreateAllocationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _journalService.CreateAllocationRuleAsync(
            request.CompanyId,
            request.Name,
            request.Description,
            request.SourceAccountId,
            request.Method,
            request.IsActive,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, MapToDto(rule));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AllocationRuleDto>> Update(Guid id, [FromBody] UpdateAllocationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _journalService.UpdateAllocationRuleAsync(
            id,
            request.Name,
            request.Description,
            request.SourceAccountId,
            request.Method,
            request.IsActive,
            cancellationToken);

        return Ok(MapToDto(rule));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<AllocationRuleDto>> AddLine(Guid id, [FromBody] AddAllocationRuleLineRequest request, CancellationToken cancellationToken)
    {
        var rule = await _journalService.AddAllocationRuleLineAsync(
            id,
            request.TargetAccountId,
            request.Percentage,
            request.FixedAmount,
            request.Reference,
            cancellationToken);

        return Ok(MapToDto(rule));
    }

    [HttpPost("{id:guid}/execute")]
    public async Task<ActionResult<JournalBatchDto>> Execute(Guid id, [FromBody] ExecuteAllocationRequest request, CancellationToken cancellationToken)
    {
        var batch = await _journalService.ExecuteAllocationAsync(
            id,
            request.BatchNumber,
            request.SourceAmount,
            request.FiscalPeriodId,
            request.PostingDate,
            cancellationToken);

        var lines = await _unitOfWork.JournalEntryLines.FindAsync(x => x.JournalBatchId == batch.Id, cancellationToken);
        return CreatedAtAction(nameof(JournalBatchController.GetById), new { id = batch.Id }, MapBatchToDto(batch, lines.ToList()));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.AllocationRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        rule.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _unitOfWork.AllocationRules.GetByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        rule.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static AllocationRuleDto MapToDto(AllocationRule rule)
    {
        var lines = rule.Lines
            .Select(l => new AllocationRuleLineDto(
                l.Id,
                l.TargetAccountId,
                l.Percentage,
                l.FixedAmount,
                l.Reference))
            .ToList();

        return new AllocationRuleDto(
            rule.Id,
            rule.CompanyId,
            rule.Name,
            rule.Description,
            rule.SourceAccountId,
            rule.Method,
            rule.IsActive,
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

public record UpdateAllocationRuleRequest(
    string Name,
    string Description,
    Guid SourceAccountId,
    AllocationMethod Method,
    bool IsActive);