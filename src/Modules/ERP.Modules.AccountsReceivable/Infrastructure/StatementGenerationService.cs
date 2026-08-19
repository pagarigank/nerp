// <copyright file="StatementGenerationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class StatementGenerationService : IStatementGenerationService
{
    private readonly ArDbContext _context;

    public StatementGenerationService(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Statement>> GenerateStatementsAsync(Guid companyId, DateTimeOffset asOfDate, CancellationToken cancellationToken = default)
    {
        var customers = await _context.Customers
            .Where(c => c.IsActive && !c.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var statements = new List<Statement>();
        foreach (var customer in customers)
        {
            var openInvoices = await _context.Invoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customer.Id
                    && i.Status != InvoiceStatus.Voided
                    && i.InvoiceDate <= asOfDate)
                .ToListAsync(cancellationToken);

            if (openInvoices.Count == 0 || openInvoices.Sum(i => i.BalanceDue) <= 0)
                continue;

            var statement = new Statement(
                companyId,
                customer.Id,
                asOfDate,
                $"STMT-{customer.CustomerId}-{asOfDate:yyyyMMdd}");

            _context.Statements.Add(statement);
            statements.Add(statement);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return statements;
    }
}
