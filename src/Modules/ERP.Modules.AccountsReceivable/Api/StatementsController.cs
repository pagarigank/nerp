// <copyright file="StatementsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/ar/statements")]
public class StatementsController : ControllerBase
{
    private readonly IStatementGenerationService _statementService;
    private readonly ArDbContext _context;

    public StatementsController(IStatementGenerationService statementService, ArDbContext context)
    {
        _statementService = statementService ?? throw new ArgumentNullException(nameof(statementService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StatementResponse>>> GetListAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.Statements.ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId);

        var statements = await query
            .Where(s => s.CompanyId == companyId && !s.DeletedOn.HasValue)
            .OrderByDescending(s => s.AsOfDate)
            .ToListAsync(cancellationToken);

        var customerIds = statements.Select(s => s.CustomerId).Distinct().ToList();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c, cancellationToken);

        var response = new List<StatementResponse>();
        foreach (var statement in statements)
        {
            var invoices = await _context.Invoices
                .Where(i => i.CustomerId == statement.CustomerId
                    && i.InvoiceDate <= statement.AsOfDate
                    && i.Status != InvoiceStatus.Voided)
                .ToListAsync(cancellationToken);

            var customer = customers.GetValueOrDefault(statement.CustomerId);
            response.Add(new StatementResponse(
                statement.Id,
                statement.CustomerId,
                customer?.CustomerId ?? string.Empty,
                customer?.Name ?? string.Empty,
                statement.StatementNumber,
                statement.AsOfDate,
                statement.Status.ToString(),
                invoices.Sum(i => i.BalanceDue)));
        }

        return Ok(response);
    }

    [HttpPost("generate")]
    public async Task<ActionResult> GenerateAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var statements = await _statementService.GenerateStatementsAsync(companyId, asOfDate, cancellationToken);
        return Ok(new { count = statements.Count, asOfDate });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetStatementAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (statement == null)
            return NotFound();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == statement.CustomerId, cancellationToken);

        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == statement.CustomerId
                && i.InvoiceDate <= statement.AsOfDate
                && i.Status != InvoiceStatus.Voided)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            statement.Id,
            statement.StatementNumber,
            statement.AsOfDate,
            statement.Status,
            CustomerName = customer?.Name ?? string.Empty,
            CustomerCode = customer?.CustomerId ?? string.Empty,
            Invoices = invoices.Select(i => new
            {
                i.InvoiceNumber,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                i.BalanceDue,
                i.Status
            }),
            TotalDue = invoices.Sum(i => i.BalanceDue)
        });
    }
}
