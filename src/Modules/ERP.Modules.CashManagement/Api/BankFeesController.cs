// <copyright file="BankFeesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cash/bank-fees")]
public class BankFeesController : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly IBankFeeService _service;

    public BankFeesController(CashDbContext context, IBankFeeService service)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankFeeResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.BankFees.AsNoTracking();

        query = query.ApplyCompanyScope(HttpContext, f => f.CompanyId, companyId);

        var fees = await query
            .OrderByDescending(f => f.FeeDate)
            .Select(f => Map(f))
            .ToListAsync(cancellationToken);

        return Ok(fees);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankFeeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var fee = await _context.BankFees
            .FirstOrDefaultAsync(f => f.Id == id && !f.DeletedOn.HasValue, cancellationToken);

        if (fee == null)
            return NotFound();

        return Ok(Map(fee));
    }

    [HttpPost]
    public async Task<ActionResult<BankFeeResponse>> RecordAsync(
        RecordBankFeeRequest request,
        CancellationToken cancellationToken)
    {
        var fee = await _service.RecordAsync(
            request.CompanyId,
            request.BankAccountId,
            request.FeeNumber,
            (BankFeeType)request.FeeType,
            request.Amount,
            request.FeeDate,
            request.Description,
            request.ExpenseGlAccountId,
            request.PostedBy ?? "admin",
            cancellationToken);

        return CreatedAtAction("GetById", new { id = fee.Id }, Map(fee));
    }

    private static BankFeeResponse Map(BankFee fee) => new(
        fee.Id,
        fee.CompanyId,
        fee.BankAccountId,
        fee.FeeNumber,
        fee.FeeType.ToString(),
        fee.Amount,
        fee.FeeDate,
        fee.Description,
        fee.GlJournalBatchId,
        fee.Status.ToString());
}
