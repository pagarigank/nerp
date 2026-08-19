// <copyright file="InvoicesController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/ar/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly ArDbContext _context;
    private readonly IPeriodService _periodService;
    private readonly ISodService _sodService;
    private readonly ICurrentUserService _currentUser;

    public InvoicesController(
        ArDbContext context,
        IPeriodService periodService,
        ISodService sodService,
        ICurrentUserService currentUser)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
        _sodService = sodService ?? throw new ArgumentNullException(nameof(sodService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IReadOnlyList<InvoiceBatchResponse>>> GetBatchesAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var batches = await _context.InvoiceBatches
            .Where(b => b.CompanyId == companyId && !b.DeletedOn.HasValue)
            .Include(b => b.Invoices)
            .ThenInclude(i => i.Lines)
            .OrderByDescending(b => b.CreatedOn)
            .ToListAsync(cancellationToken);

        var result = batches.Select(b => new InvoiceBatchResponse(
            b.Id,
            b.BatchNumber,
            b.Description,
            b.Status.ToString(),
            b.Invoices.Count,
            b.Invoices.Sum(i => i.TotalAmount))).ToList();

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceResponse>>> GetListAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var batchIds = await _context.InvoiceBatches
            .Where(b => b.CompanyId == companyId && !b.DeletedOn.HasValue)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var query = _context.Invoices.Where(i => batchIds.Contains(i.InvoiceBatchId));
        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);

        var invoices = await query
            .Include(i => i.Lines)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        var response = invoices.Select(i => new InvoiceResponse(
            i.Id,
            i.CustomerId,
            i.InvoiceNumber,
            i.InvoiceDate,
            i.DueDate,
            i.TotalAmount,
            i.BalanceDue,
            i.Status.ToString()))
            .ToList();

        return Ok(response);
    }

    [HttpGet("batches/{batchId:guid}")]
    public async Task<ActionResult<InvoiceBatchDetailResponse>> GetBatchByIdAsync(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var batch = await _context.InvoiceBatches
            .Include(b => b.Invoices)
            .ThenInclude(i => i.Lines)
            .FirstOrDefaultAsync(b => b.Id == batchId && !b.DeletedOn.HasValue, cancellationToken);

        if (batch == null)
            return NotFound();

        var customerIds = batch.Invoices.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var invoices = batch.Invoices.Select(i => new InvoiceDetailResponse(
            i.Id,
            i.CustomerId,
            customers.GetValueOrDefault(i.CustomerId, string.Empty),
            i.InvoiceNumber,
            i.InvoiceDate,
            i.DueDate,
            i.Description,
            i.Status.ToString(),
            i.TotalAmount,
            i.BalanceDue,
            i.Lines.Select(l => new InvoiceLineDetailResponse(
                l.AccountId,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.TaxAmount,
                l.DiscountAmount,
                l.TotalAmount)).ToList()))
            .ToList();

        return Ok(new InvoiceBatchDetailResponse(
            batch.Id,
            batch.BatchNumber,
            batch.Description,
            batch.Status.ToString(),
            batch.PostingDate,
            invoices));
    }

    [HttpPost("batches")]
    public async Task<ActionResult<InvoiceBatchResponse>> CreateBatchAsync(
        CreateInvoiceBatchRequest request,
        CancellationToken cancellationToken)
    {
        var batch = new InvoiceBatch(
            request.CompanyId,
            request.BatchNumber,
            request.Description,
            request.PostingDate,
            request.FiscalPeriodId);

        _context.InvoiceBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetBatches", new { companyId = request.CompanyId }, new InvoiceBatchResponse(
            batch.Id,
            batch.BatchNumber,
            batch.Description,
            batch.Status.ToString(),
            0,
            0));
    }

    [HttpPost("batches/{batchId:guid}/lines")]
    public async Task<ActionResult> AddInvoiceLinesAsync(
        Guid batchId,
        IReadOnlyList<InvoiceBatchLineItem> lineItems,
        CancellationToken cancellationToken)
    {
        var batch = await _context.InvoiceBatches
            .FirstOrDefaultAsync(b => b.Id == batchId && b.Status == InvoiceBatchStatus.Draft, cancellationToken);

        if (batch == null)
            return NotFound("Batch not found or not in Draft status.");

        foreach (var item in lineItems)
        {
            var invoice = batch.AddInvoice(
                item.CustomerId,
                item.InvoiceNumber,
                item.InvoiceDate,
                item.DueDate,
                item.Description,
                item.PaymentTermId,
                item.ProjectId,
                item.SalesOrderId);

            foreach (var line in item.Lines)
            {
                invoice.AddLine(
                    line.AccountId,
                    line.Description,
                    line.Quantity,
                    line.UnitPrice,
                    line.TaxAmount,
                    line.DiscountAmount);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("batches/{batchId:guid}/release")]
    public async Task<ActionResult> ReleaseBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await _context.InvoiceBatches
            .Include(b => b.Invoices)
            .ThenInclude(i => i.Lines)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
            return NotFound();

        try
        {
            batch.Release();
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("batches/{batchId:guid}/post")]
    public async Task<ActionResult> PostBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await _context.InvoiceBatches
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
            return NotFound();

        try
        {
            if (!await _periodService.IsPeriodOpenAsync(batch.CompanyId, batch.PostingDate, cancellationToken))
                return BadRequest($"Cannot post invoice batch {batch.BatchNumber}: fiscal period for {batch.PostingDate:yyyy-MM-dd} is not open.");

            // Separation of Duties: the user who created the batch must not also
            // post it. The create action is recorded by the audit save interceptor,
            // so we reject posting when the same user created the batch.
            if (!string.IsNullOrEmpty(_currentUser.UserId)
                && await _sodService.CheckConflictAsync(
                    "AccountsReceivable", nameof(InvoiceBatch), _currentUser.UserId, "Post", 0, cancellationToken))
            {
                return BadRequest($"Separation of Duties conflict: user {_currentUser.UserId} created invoice batch {batch.BatchNumber} and may not also post it.");
            }

            batch.Post();
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> VoidInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null)
            return NotFound();

        try
        {
            invoice.Void();
            await _context.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("standalone")]
    public async Task<ActionResult<InvoiceBatchResponse>> CreateStandaloneInvoiceAsync(
        CreateStandaloneInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var batch = new InvoiceBatch(
            request.CompanyId,
            $"SI-{request.InvoiceNumber}",
            $"Standalone invoice: {request.InvoiceNumber}",
            request.InvoiceDate,
            Guid.NewGuid());

        _context.InvoiceBatches.Add(batch);

        var invoice = batch.AddInvoice(
            request.CustomerId,
            request.InvoiceNumber,
            request.InvoiceDate,
            request.DueDate,
            request.Description,
            request.PaymentTermId,
            request.ProjectId,
            request.SalesOrderId);

        foreach (var line in request.Lines)
        {
            invoice.AddLine(
                line.AccountId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.TaxAmount,
                line.DiscountAmount);
        }

        batch.Release();
        batch.Post();

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetBatches", new { companyId = request.CompanyId }, new InvoiceBatchResponse(
            batch.Id,
            batch.BatchNumber,
            batch.Description,
            batch.Status.ToString(),
            1,
            invoice.TotalAmount));
    }

    [HttpPost("{id:guid}/write-off")]
    public async Task<ActionResult> WriteOffInvoiceAsync(
        Guid id,
        WriteOffRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null)
            return NotFound();

        if (string.IsNullOrEmpty(request.ApprovalToken))
            return BadRequest("Write-off requires an approval token from the workflow engine.");

        try
        {
            invoice.WriteOff(request.Amount, request.Reason);
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
}
