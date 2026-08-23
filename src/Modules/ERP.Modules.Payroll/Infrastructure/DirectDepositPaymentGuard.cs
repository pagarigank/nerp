// <copyright file="DirectDepositPaymentGuard.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;

namespace ERP.Modules.Payroll.Infrastructure;

/// <summary>
/// Prenote enforcement for pay-instrument generation (spec §5.12): an employee's direct
/// deposits may only fund live ACH entries when at least one account is bank-verified or
/// has had a pre-note outstanding for two completed payroll cycles. Employees without any
/// direct-deposit record are paper-check payees and are never blocked.
/// </summary>
public static class DirectDepositPaymentGuard
{
    public static bool IsEmployeeEligible(
        IReadOnlyCollection<DirectDeposit> employeeDeposits,
        IReadOnlyCollection<DateTime> postedPayDates,
        DateTimeOffset asOf)
    {
        if (employeeDeposits.Count == 0)
            return true;

        foreach (var deposit in employeeDeposits)
        {
            var cycles = CountCompletedCycles(deposit.PrenoteSentOn, postedPayDates);
            if (DirectDeposit.IsEligibleForPayment(deposit.VerifiedOn, deposit.PrenoteSentOn, asOf, cycles))
                return true;
        }

        return false;
    }

    public static int CountCompletedCycles(DateTimeOffset? prenoteSentOn, IReadOnlyCollection<DateTime> postedPayDates)
    {
        if (!prenoteSentOn.HasValue)
            return 0;
        var sent = prenoteSentOn.Value;
        var count = 0;
        foreach (var payDate in postedPayDates)
        {
            if (new DateTimeOffset(DateTime.SpecifyKind(payDate, DateTimeKind.Utc)) > sent)
                count++;
        }

        return count;
    }
}
