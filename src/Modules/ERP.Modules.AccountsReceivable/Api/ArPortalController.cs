// <copyright file="ArPortalController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar/portal")]
public class ArPortalController : ControllerBase
{
    private readonly ArDbContext _context;

    public ArPortalController(ArDbContext context)
    {
        _context = context;
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<List<PortalInvoiceDto>>>> GetInvoices(
        [FromQuery] Guid companyId,
        [FromQuery] Guid customerId,
        CancellationToken ct)
    {
        var batches = await _context.InvoiceBatches
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Id)
            .ToListAsync(ct);

        var list = await _context.Invoices
            .Where(i => batches.Contains(i.InvoiceBatchId) && i.CustomerId == customerId)
            .Select(i => new PortalInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                BalanceDue = i.BalanceDue,
                Status = i.Status.ToString(),
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<PortalInvoiceDto>>.Success(list));
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PortalInvoiceDetailDto>>> GetInvoice(
        Guid id,
        [FromQuery] Guid companyId,
        [FromQuery] Guid customerId,
        CancellationToken ct)
    {
        var batches = await _context.InvoiceBatches
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Id)
            .ToListAsync(ct);

        var inv = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && batches.Contains(i.InvoiceBatchId) && i.CustomerId == customerId, ct);

        if (inv is null)
        {
            return NotFound(ApiResponse<PortalInvoiceDetailDto>.Failure(new[] { "Invoice not found." }));
        }

        var dto = new PortalInvoiceDetailDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            TotalAmount = inv.TotalAmount,
            BalanceDue = inv.BalanceDue,
            Status = inv.Status.ToString(),
            Lines = inv.Lines.Select(l => new PortalInvoiceLineDto
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.TotalAmount,
            }).ToList(),
        };

        return Ok(ApiResponse<PortalInvoiceDetailDto>.Success(dto));
    }

    [HttpGet("statements")]
    public async Task<ActionResult<ApiResponse<List<PortalStatementDto>>>> GetStatements(
        [FromQuery] Guid companyId,
        [FromQuery] Guid customerId,
        CancellationToken ct)
    {
        var list = await _context.Statements
            .Where(s => s.CompanyId == companyId && s.CustomerId == customerId)
            .Select(s => new PortalStatementDto
            {
                Id = s.Id,
                StatementNumber = s.StatementNumber,
                AsOfDate = s.AsOfDate,
                Status = s.Status.ToString(),
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<PortalStatementDto>>.Success(list));
    }

    [HttpGet("statements/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PortalStatementDto>>> GetStatement(
        Guid id,
        [FromQuery] Guid companyId,
        [FromQuery] Guid customerId,
        CancellationToken ct)
    {
        var s = await _context.Statements
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.CustomerId == customerId, ct);

        if (s is null)
        {
            return NotFound(ApiResponse<PortalStatementDto>.Failure(new[] { "Statement not found." }));
        }

        var dto = new PortalStatementDto
        {
            Id = s.Id,
            StatementNumber = s.StatementNumber,
            AsOfDate = s.AsOfDate,
            Status = s.Status.ToString(),
        };

        return Ok(ApiResponse<PortalStatementDto>.Success(dto));
    }
}

public record PortalInvoiceDto
{
    public Guid Id { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; init; }

    public DateTimeOffset DueDate { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal BalanceDue { get; init; }

    public string Status { get; init; } = string.Empty;
}

public record PortalInvoiceDetailDto
{
    public Guid Id { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public DateTimeOffset InvoiceDate { get; init; }

    public DateTimeOffset DueDate { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal BalanceDue { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyCollection<PortalInvoiceLineDto> Lines { get; init; } = new List<PortalInvoiceLineDto>();
}

public record PortalInvoiceLineDto
{
    public string Description { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}

public record PortalStatementDto
{
    public Guid Id { get; init; }

    public string StatementNumber { get; init; } = string.Empty;

    public DateTimeOffset AsOfDate { get; init; }

    public string Status { get; init; } = string.Empty;
}
