// <copyright file="PayrollPostedToCashHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="PayrollPostedEvent"/> (raised when a Payroll run is posted) and records
/// each issued pay instrument as an outstanding reconcilable item in Cash Management (Phase 11
/// item #1102). This is the payroll -> Cash Management positive-pay / bank reconciliation link.
/// The event lives in ERP.Core, so Cash Management references only the shared contract (no cycle).
/// </summary>
public sealed class PayrollPostedToCashHandler : IDomainEventHandler<PayrollPostedEvent>
{
    private readonly CashDbContext _cashContext;

    public PayrollPostedToCashHandler(CashDbContext cashContext)
    {
        _cashContext = cashContext ?? throw new ArgumentNullException(nameof(cashContext));
    }

    public async Task HandleAsync(PayrollPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Skip if already recorded for this run (idempotent re-delivery).
        var already = await _cashContext.PayrollCheckIssues
            .AnyAsync(i => i.PayrollRunId == domainEvent.PayrollRunId, cancellationToken);
        if (already)
            return;

        foreach (var payment in domainEvent.Payments)
        {
            _cashContext.PayrollCheckIssues.Add(new PayrollCheckIssue(
                companyId: domainEvent.CompanyId,
                payrollRunId: domainEvent.PayrollRunId,
                employeeId: payment.EmployeeId,
                paymentMethod: payment.PaymentMethod,
                checkNumber: payment.CheckNumber,
                amount: payment.Amount,
                issuedOn: domainEvent.PayDate,
                bankAccountLast4: payment.BankAccountLast4));
        }

        await _cashContext.SaveChangesAsync(cancellationToken);
    }
}
