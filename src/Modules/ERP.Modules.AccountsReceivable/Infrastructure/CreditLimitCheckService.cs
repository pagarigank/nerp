// <copyright file="CreditLimitCheckService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public class CreditLimitCheckService : ICreditLimitCheckService
{
    private readonly ArDbContext _context;

    public CreditLimitCheckService(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CreditLimitCheckResult> CheckAsync(Guid customerId, decimal proposedAmount, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
            return new CreditLimitCheckResult(false, 0, 0, 0, "Customer not found.");

        var openInvoices = await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == customerId
                && (i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync(cancellationToken);

        var currentBalance = openInvoices.Sum(i => i.BalanceDue);

        var availableCredit = customer.CreditLimit - currentBalance;
        var isApproved = availableCredit >= proposedAmount;

        return new CreditLimitCheckResult(
            isApproved,
            currentBalance,
            customer.CreditLimit,
            Math.Max(0, availableCredit),
            isApproved ? null : $"Proposed amount {proposedAmount:C2} exceeds available credit of {Math.Max(0, availableCredit):C2}.");
    }
}
