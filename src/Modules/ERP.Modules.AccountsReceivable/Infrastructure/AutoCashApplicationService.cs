// <copyright file="AutoCashApplicationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class AutoCashApplicationService : IAutoCashApplicationService
{
    private readonly ArDbContext _context;

    public AutoCashApplicationService(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<CashReceiptApplication>> AutoApplyAsync(Guid cashReceiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.CashReceipts
            .Include(r => r.Applications)
            .FirstOrDefaultAsync(r => r.Id == cashReceiptId, cancellationToken);

        if (receipt == null)
            throw new InvalidOperationException($"Cash receipt {cashReceiptId} not found.");

        if (receipt.UnappliedAmount <= 0)
            return [];

        var openInvoices = await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == receipt.CustomerId
                && (i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid))
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        var applications = new List<CashReceiptApplication>();
        var remaining = receipt.UnappliedAmount;

        foreach (var invoice in openInvoices)
        {
            if (remaining <= 0)
                break;

            var applyAmount = Math.Min(remaining, invoice.BalanceDue);
            if (applyAmount <= 0)
                continue;

            var application = receipt.ApplyToInvoice(invoice, applyAmount);
            applications.Add(application);
            remaining -= applyAmount;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return applications;
    }
}
