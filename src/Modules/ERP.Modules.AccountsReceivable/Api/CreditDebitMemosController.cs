// <copyright file="CreditDebitMemosController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/memos")]
public class CreditDebitMemosController : ControllerBase
{
    private readonly ArDbContext _context;

    public CreditDebitMemosController(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemoResponse>>> GetMemosAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.InvoiceBatches.ApplyCompanyScope(HttpContext, b => b.CompanyId, companyId);

        var batchIds = await query
            .Where(b => b.CompanyId == companyId && !b.DeletedOn.HasValue)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var memos = await _context.CreditDebitMemos
            .Where(m => batchIds.Contains(m.InvoiceBatchId))
            .OrderByDescending(m => m.MemoDate)
            .Select(m => new MemoResponse(
                m.Id,
                m.CustomerId,
                m.ReferenceNumber,
                m.MemoDate,
                m.MemoType.ToString(),
                m.Status.ToString(),
                m.TotalAmount,
                m.Description))
            .ToListAsync(cancellationToken);

        return Ok(memos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemoResponse>> GetMemoAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var memo = await _context.CreditDebitMemos
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (memo == null)
            return NotFound();

        return Ok(new MemoResponse(
            memo.Id,
            memo.CustomerId,
            memo.ReferenceNumber,
            memo.MemoDate,
            memo.MemoType.ToString(),
            memo.Status.ToString(),
            memo.TotalAmount,
            memo.Description));
    }

    [HttpPost]
    public async Task<ActionResult<MemoResponse>> CreateMemoAsync(
        CreateMemoRequest request,
        CancellationToken cancellationToken)
    {
        var batch = new InvoiceBatch(
            request.CompanyId,
            $"SM-{request.ReferenceNumber}",
            $"Standalone memo: {request.ReferenceNumber}",
            request.MemoDate,
            Guid.NewGuid());

        _context.InvoiceBatches.Add(batch);

        var memo = batch.AddCreditDebitMemo(
            request.CustomerId,
            request.ReferenceNumber,
            request.MemoDate,
            request.InvoiceId,
            request.Description);

        memo.SetMemoType((CreditDebitMemoType)request.MemoType);

        // The InvoiceBatch -> CreditDebitMemo relationship is not configured for cascade
        // persistence, so the memo must be tracked explicitly or SaveChanges skips it.
        _context.CreditDebitMemos.Add(memo);

        foreach (var line in request.Lines)
        {
            memo.AddLine(
                line.AccountId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.TaxAmount,
                line.DiscountAmount);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetMemo", new { id = memo.Id }, new MemoResponse(
            memo.Id,
            memo.CustomerId,
            memo.ReferenceNumber,
            memo.MemoDate,
            memo.MemoType.ToString(),
            memo.Status.ToString(),
            memo.TotalAmount,
            memo.Description));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MemoResponse>> UpdateMemoAsync(
        Guid id,
        UpdateMemoRequest request,
        CancellationToken cancellationToken)
    {
        var memo = await _context.CreditDebitMemos
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (memo == null)
            return NotFound();

        if (memo.Status != CreditDebitMemoStatus.Open)
            return BadRequest("Only Open memos can be updated.");

        memo.SetMemoType((CreditDebitMemoType)request.MemoType);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new MemoResponse(
            memo.Id,
            memo.CustomerId,
            memo.ReferenceNumber,
            memo.MemoDate,
            memo.MemoType.ToString(),
            memo.Status.ToString(),
            memo.TotalAmount,
            memo.Description));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteMemoAsync(Guid id, CancellationToken cancellationToken)
    {
        var memo = await _context.CreditDebitMemos
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (memo == null)
            return NotFound();

        if (memo.Status == CreditDebitMemoStatus.Applied)
            return BadRequest("Applied memos cannot be deleted. Void instead.");

        try
        {
            memo.Void();
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
