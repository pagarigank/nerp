// <copyright file="BankTransfersController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/cash/transfers")]
public class BankTransfersController : ControllerBase
{
    private readonly CashDbContext _context;

    public BankTransfersController(CashDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankTransferResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.BankTransfers.AsNoTracking();

        if (companyId.HasValue)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var transfers = await query
            .OrderByDescending(t => t.TransferDate)
            .Select(t => Map(t))
            .ToListAsync(cancellationToken);

        return Ok(transfers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankTransferResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await _context.BankTransfers
            .FirstOrDefaultAsync(t => t.Id == id && !t.DeletedOn.HasValue, cancellationToken);

        if (transfer == null)
            return NotFound();

        return Ok(Map(transfer));
    }

    [HttpPost]
    public async Task<ActionResult<BankTransferResponse>> CreateAsync(
        CreateBankTransferRequest request,
        CancellationToken cancellationToken)
    {
        var transfer = new BankTransfer(
            request.CompanyId,
            request.FromBankAccountId,
            request.ToBankAccountId,
            request.TransferNumber,
            request.Amount,
            request.TransferDate,
            request.Reference);

        transfer.CreatedBy = "admin";
        _context.BankTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction("GetById", new { id = transfer.Id }, Map(transfer));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<BankTransferResponse>> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await GetTransferAsync(id, cancellationToken);

        var fromAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == transfer.FromBankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Source bank account {transfer.FromBankAccountId} not found.");
        var toAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == transfer.ToBankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Destination bank account {transfer.ToBankAccountId} not found.");

        transfer.Confirm();
        fromAccount.AdjustBalance(-transfer.Amount);
        toAccount.AdjustBalance(transfer.Amount);
        transfer.MarkModified("admin");
        fromAccount.MarkModified("admin");
        toAccount.MarkModified("admin");

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(transfer));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<BankTransferResponse>> CompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await GetTransferAsync(id, cancellationToken);

        transfer.Complete();
        transfer.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(transfer));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<BankTransferResponse>> VoidAsync(Guid id, string? reason, CancellationToken cancellationToken)
    {
        var transfer = await GetTransferAsync(id, cancellationToken);

        transfer.Void(reason);
        transfer.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(transfer));
    }

    private static BankTransferResponse Map(BankTransfer transfer) => new(
        transfer.Id,
        transfer.CompanyId,
        transfer.FromBankAccountId,
        transfer.ToBankAccountId,
        transfer.TransferNumber,
        transfer.Amount,
        transfer.TransferDate,
        transfer.Reference,
        transfer.Status.ToString());

    private async Task<BankTransfer> GetTransferAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.BankTransfers
            .FirstOrDefaultAsync(t => t.Id == id && !t.DeletedOn.HasValue, cancellationToken)
            ?? throw new InvalidOperationException($"Bank transfer {id} not found.");
    }
}
