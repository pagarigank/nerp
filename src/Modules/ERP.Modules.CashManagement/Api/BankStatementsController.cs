// <copyright file="BankStatementsController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/cash/bank-statements")]
public class BankStatementsController : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly IBankStatementParserService _parser;

    public BankStatementsController(CashDbContext context, IBankStatementParserService parser)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankStatementResponse>>> GetListAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? bankAccountId,
        CancellationToken cancellationToken)
    {
        var query = _context.BankStatements.AsNoTracking();

        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        if (bankAccountId.HasValue)
        {
            query = query.Where(s => s.BankAccountId == bankAccountId.Value);
        }

        var statements = await query
            .OrderByDescending(s => s.StatementDate)
            .Select(s => new BankStatementResponse(
                s.Id,
                s.CompanyId,
                s.BankAccountId,
                s.StatementNumber,
                s.StatementDate,
                s.BeginningBalance,
                s.EndingBalance,
                s.Format.ToString(),
                s.Status.ToString(),
                s.Lines.Count))
            .ToListAsync(cancellationToken);

        return Ok(statements);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankStatementDetailResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var statement = await _context.BankStatements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id && !s.DeletedOn.HasValue, cancellationToken);

        if (statement == null)
            return NotFound();

        return Ok(new BankStatementDetailResponse(
            new BankStatementResponse(
                statement.Id,
                statement.CompanyId,
                statement.BankAccountId,
                statement.StatementNumber,
                statement.StatementDate,
                statement.BeginningBalance,
                statement.EndingBalance,
                statement.Format.ToString(),
                statement.Status.ToString(),
                statement.Lines.Count),
            statement.Lines
                .OrderBy(l => l.TransactionDate)
                .Select(l => new BankStatementLineResponse(
                    l.Id,
                    l.TransactionDate,
                    l.Amount,
                    l.Description,
                    l.ReferenceNumber,
                    l.CheckNumber,
                    l.Balance,
                    l.Status.ToString(),
                    l.MatchedTransactionId,
                    l.MatchedSource?.ToString()))
                .ToList()));
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportStatementResponse>> ImportAsync(
        ImportStatementRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = _parser.Parse(request.FileContent, request.Format.HasValue ? (BankStatementFormat)request.Format.Value : null);

        var statement = new BankStatement(
            request.CompanyId,
            request.BankAccountId,
            request.StatementNumber,
            request.StatementDate,
            parsed.BeginningBalance ?? 0,
            parsed.EndingBalance ?? 0,
            $"statement-{request.StatementNumber}.{parsed.Format.ToString().ToUpperInvariant()}",
            parsed.Format);

        statement.CreatedBy = "admin";

        foreach (var line in parsed.Lines)
        {
            statement.AddLine(
                line.TransactionDate,
                line.Amount,
                line.Description,
                line.ReferenceNumber,
                line.CheckNumber,
                line.Balance ?? 0);
        }

        if (parsed.Lines.Count > 0)
        {
            statement.MarkValidated();
        }

        _context.BankStatements.Add(statement);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ImportStatementResponse(
            statement.Id,
            statement.StatementNumber,
            statement.Format.ToString(),
            statement.Lines.Count,
            parsed.BeginningBalance,
            parsed.EndingBalance,
            parsed.Warnings));
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<ActionResult<BankStatementResponse>> ValidateAsync(Guid id, CancellationToken cancellationToken)
    {
        var statement = await _context.BankStatements
            .FirstOrDefaultAsync(s => s.Id == id && !s.DeletedOn.HasValue, cancellationToken);

        if (statement == null)
            return NotFound();

        if (statement.Status == BankStatementStatus.Imported)
        {
            statement.MarkValidated();
            statement.MarkModified("admin");
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new BankStatementResponse(
            statement.Id,
            statement.CompanyId,
            statement.BankAccountId,
            statement.StatementNumber,
            statement.StatementDate,
            statement.BeginningBalance,
            statement.EndingBalance,
            statement.Format.ToString(),
            statement.Status.ToString(),
            statement.Lines.Count));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var statement = await _context.BankStatements
            .FirstOrDefaultAsync(s => s.Id == id && !s.DeletedOn.HasValue, cancellationToken);

        if (statement == null)
            return NotFound();

        if (statement.Status == BankStatementStatus.Locked)
            return Conflict(new { message = "A locked statement cannot be deleted." });

        statement.MarkDeleted("admin");
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
