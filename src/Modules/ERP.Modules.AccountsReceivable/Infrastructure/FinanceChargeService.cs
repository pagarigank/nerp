// <copyright file="FinanceChargeService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class FinanceChargeService : IFinanceChargeService
{
    private readonly ArDbContext _context;

    public FinanceChargeService(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<FinanceCharge>> CalculateChargesAsync(Guid companyId, decimal annualRate, DateTimeOffset asOfDate, CancellationToken cancellationToken = default)
    {
        var customers = await _context.Customers
            .Where(c => c.IsActive && !c.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var charges = new List<FinanceCharge>();
        var chargeIndex = 0;

        foreach (var customer in customers)
        {
            var overdueInvoices = await _context.Invoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customer.Id
                    && i.Status == InvoiceStatus.Open
                    && i.DueDate < asOfDate)
                .ToListAsync(cancellationToken);

            if (overdueInvoices.Count == 0)
                continue;

            var totalOverdue = overdueInvoices.Sum(i => i.BalanceDue);
            if (totalOverdue <= 0)
                continue;

            var monthlyRate = annualRate / 12 / 100;
            var chargeAmount = Math.Round(totalOverdue * monthlyRate, 2);

            if (chargeAmount <= 0)
                continue;

            chargeIndex++;
            var charge = new FinanceCharge(
                companyId,
                customer.Id,
                $"FC-{asOfDate:yyyyMMdd}-{chargeIndex:D4}",
                asOfDate,
                chargeAmount,
                annualRate,
                $"Finance charge on overdue balance of {totalOverdue:C2}");

            _context.FinanceCharges.Add(charge);
            charges.Add(charge);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return charges;
    }
}
