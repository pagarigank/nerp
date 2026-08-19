// <copyright file="CashReceiptsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/cash-receipts")]
public class CashReceiptsController : ControllerBase
{
    private readonly ArDbContext _context;
    private readonly IAutoCashApplicationService _autoApplyService;

    public CashReceiptsController(ArDbContext context, IAutoCashApplicationService autoApplyService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _autoApplyService = autoApplyService ?? throw new ArgumentNullException(nameof(autoApplyService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashReceiptResponse>>> GetListAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var receipts = await _context.CashReceipts
            .Where(r => r.CompanyId == companyId && !r.DeletedOn.HasValue)
            .Include(r => r.Applications)
            .OrderByDescending(r => r.ReceiptDate)
            .Select(r => new CashReceiptResponse(
                r.Id,
                r.CustomerId,
                r.ReceiptReference,
                r.TotalAmount,
                r.AppliedAmount,
                r.UnappliedAmount,
                r.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(receipts);
    }

    [HttpPost]
    public async Task<ActionResult<CashReceiptResponse>> CreateAsync(
        CreateCashReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = new CashReceipt(
            request.CompanyId,
            request.CustomerId,
            request.ReceiptReference,
            request.TotalAmount,
            request.ReceiptDate,
            request.PaymentMethod,
            request.CurrencyCode,
            request.ReferenceNumber);

        _context.CashReceipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(null, new CashReceiptResponse(
            receipt.Id,
            receipt.CustomerId,
            receipt.ReceiptReference,
            receipt.TotalAmount,
            0,
            receipt.TotalAmount,
            receipt.Status.ToString()));
    }

    [HttpPost("{id:guid}/apply")]
    public async Task<ActionResult> ApplyToInvoiceAsync(
        Guid id,
        ApplyCashRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = await _context.CashReceipts
            .Include(r => r.Applications)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt == null)
            return NotFound("Cash receipt not found.");

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            return NotFound("Invoice not found.");

        try
        {
            receipt.ApplyToInvoice(invoice, request.Amount);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/auto-apply")]
    public async Task<ActionResult<IReadOnlyList<CashReceiptApplicationResponse>>> AutoApplyAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _autoApplyService.AutoApplyAsync(id, cancellationToken);
            return Ok(applications.Select(a => new CashReceiptApplicationResponse(
                a.Id,
                a.CashReceiptId,
                a.InvoiceId,
                a.AppliedAmount)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
