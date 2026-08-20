// <copyright file="NsfController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/cash/nsf")]
public class NsfController : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly INsfService _service;

    public NsfController(CashDbContext context, INsfService service)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NsfResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.NsfRecords.AsNoTracking();

        query = query.ApplyCompanyScope(HttpContext, n => n.CompanyId, companyId);

        var records = await query
            .OrderByDescending(n => n.ReturnedDate)
            .Select(n => Map(n))
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NsfResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await _context.NsfRecords
            .FirstOrDefaultAsync(n => n.Id == id && !n.DeletedOn.HasValue, cancellationToken);

        if (record == null)
            return NotFound();

        return Ok(Map(record));
    }

    [HttpPost]
    public async Task<ActionResult<NsfResponse>> ProcessAsync(
        ProcessNsfRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _service.ProcessAsync(
            request.CompanyId,
            request.BankAccountId,
            request.CashReceiptId,
            request.NsfNumber,
            request.Amount,
            request.ReturnedDate,
            request.BankReference,
            request.Reason,
            request.NsfFeeAmount,
            request.ProcessedBy ?? "admin",
            cancellationToken);

        return CreatedAtAction("GetById", new { id = record.Id }, Map(record));
    }

    private static NsfResponse Map(NsfRecord record) => new(
        record.Id,
        record.CompanyId,
        record.BankAccountId,
        record.CashReceiptId,
        record.CustomerId,
        record.NsfNumber,
        record.Amount,
        record.ReturnedDate,
        record.BankReference,
        record.Reason,
        record.NsfFeeAmount,
        record.Status.ToString());
}
