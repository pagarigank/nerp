// <copyright file="DepositsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cash/deposits")]
public class DepositsController : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly ArDbContext _arContext;

    public DepositsController(CashDbContext context, ArDbContext arContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepositResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? bankAccountId,
        CancellationToken cancellationToken)
    {
        var query = _context.Deposits.AsNoTracking();

        if (companyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }

        if (bankAccountId.HasValue)
        {
            query = query.Where(d => d.BankAccountId == bankAccountId.Value);
        }

        var deposits = await query
            .Include(d => d.Lines)
            .OrderByDescending(d => d.DepositDate)
            .Select(d => new DepositResponse(
                d.Id,
                d.CompanyId,
                d.BankAccountId,
                d.DepositNumber,
                d.DepositDate,
                d.Reference,
                d.Status.ToString(),
                d.TotalAmount))
            .ToListAsync(cancellationToken);

        return Ok(deposits);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DepositDetailResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var deposit = await _context.Deposits
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id && !d.DeletedOn.HasValue, cancellationToken);

        if (deposit == null)
            return NotFound();

        return Ok(new DepositDetailResponse(
            new DepositResponse(
                deposit.Id,
                deposit.CompanyId,
                deposit.BankAccountId,
                deposit.DepositNumber,
                deposit.DepositDate,
                deposit.Reference,
                deposit.Status.ToString(),
                deposit.TotalAmount),
            deposit.Lines.Select(l => new DepositLineDetailResponse(
                l.Id,
                l.Source.ToString(),
                l.SourceReferenceId,
                l.Amount,
                l.Description)).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<DepositDetailResponse>> CreateAsync(
        CreateDepositRequest request,
        CancellationToken cancellationToken)
    {
        var deposit = new Deposit(
            request.CompanyId,
            request.BankAccountId,
            request.DepositNumber,
            request.DepositDate,
            request.Reference);

        deposit.CreatedBy = "admin";

        foreach (var line in request.Lines)
        {
            deposit.AddLine(
                (DepositLineSource)line.Source,
                line.SourceReferenceId,
                line.Amount,
                line.Description);
        }

        _context.Deposits.Add(deposit);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetById", new { id = deposit.Id }, Map(deposit));
    }

    [HttpPost("from-ar")]
    public async Task<ActionResult<DepositDetailResponse>> CreateFromArAsync(
        CreateDepositFromArRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = await _arContext.CashReceipts
            .FirstOrDefaultAsync(r => r.Id == request.CashReceiptId && !r.DeletedOn.HasValue, cancellationToken)
            ?? throw new InvalidOperationException($"Cash receipt {request.CashReceiptId} not found.");

        var deposit = new Deposit(
            request.CompanyId,
            request.BankAccountId,
            request.DepositNumber,
            request.DepositDate,
            $"From AR cash receipt {receipt.ReceiptReference}");

        deposit.CreatedBy = "admin";
        deposit.AddLine(DepositLineSource.ArCashReceipt, receipt.Id, receipt.TotalAmount, receipt.ReceiptReference);

        _context.Deposits.Add(deposit);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetById", new { id = deposit.Id }, Map(deposit));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<DepositResponse>> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var deposit = await _context.Deposits
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id && !d.DeletedOn.HasValue, cancellationToken);

        if (deposit == null)
            return NotFound();

        deposit.Confirm();
        deposit.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new DepositResponse(
            deposit.Id,
            deposit.CompanyId,
            deposit.BankAccountId,
            deposit.DepositNumber,
            deposit.DepositDate,
            deposit.Reference,
            deposit.Status.ToString(),
            deposit.TotalAmount));
    }

    [HttpPost("{id:guid}/clear")]
    public async Task<ActionResult<DepositResponse>> ClearAsync(Guid id, CancellationToken cancellationToken)
    {
        var deposit = await _context.Deposits
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id && !d.DeletedOn.HasValue, cancellationToken);

        if (deposit == null)
            return NotFound();

        deposit.Clear();
        deposit.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new DepositResponse(
            deposit.Id,
            deposit.CompanyId,
            deposit.BankAccountId,
            deposit.DepositNumber,
            deposit.DepositDate,
            deposit.Reference,
            deposit.Status.ToString(),
            deposit.TotalAmount));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deposit = await _context.Deposits
            .FirstOrDefaultAsync(d => d.Id == id && !d.DeletedOn.HasValue, cancellationToken);

        if (deposit == null)
            return NotFound();

        deposit.MarkDeleted("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static DepositDetailResponse Map(Deposit deposit) => new(
        new DepositResponse(
            deposit.Id,
            deposit.CompanyId,
            deposit.BankAccountId,
            deposit.DepositNumber,
            deposit.DepositDate,
            deposit.Reference,
            deposit.Status.ToString(),
            deposit.TotalAmount),
        deposit.Lines.Select(l => new DepositLineDetailResponse(
            l.Id,
            l.Source.ToString(),
            l.SourceReferenceId,
            l.Amount,
            l.Description)).ToList());
}
