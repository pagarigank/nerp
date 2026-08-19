// <copyright file="CashPositionJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public record AccountCashPosition(
    Guid BankAccountId,
    string AccountCode,
    string AccountName,
    string AccountNumber,
    decimal CurrentBalance,
    string CurrencyCode,
    int OutstandingChecks,
    int OutstandingDeposits);

public interface ICashPositionJob
{
    Task<IReadOnlyList<AccountCashPosition>> RunAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public class CashPositionJob : ICashPositionJob
{
    private readonly CashDbContext _context;
    private readonly IPositivePayService _positivePayService;

    public CashPositionJob(CashDbContext context, IPositivePayService positivePayService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _positivePayService = positivePayService ?? throw new ArgumentNullException(nameof(positivePayService));
    }

    public async Task<IReadOnlyList<AccountCashPosition>> RunAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var accounts = await _context.BankAccounts
            .Where(a => a.CompanyId == companyId && a.Status == BankAccountStatus.Active)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var positions = new List<AccountCashPosition>();

        foreach (var account in accounts)
        {
            var asOf = DateTimeOffset.UtcNow;
            var outstandingChecks = await _positivePayService.GetOutstandingChecksAsync(companyId, account.Id, asOf, cancellationToken);
            var outstandingDeposits = await _context.Deposits
                .CountAsync(d => d.CompanyId == companyId
                    && d.BankAccountId == account.Id
                    && d.Status == DepositStatus.Confirmed, cancellationToken);

            positions.Add(new AccountCashPosition(
                account.Id,
                account.AccountCode,
                account.AccountName,
                account.AccountNumber,
                account.CurrentBalance,
                account.CurrencyCode,
                outstandingChecks.Count,
                outstandingDeposits));
        }

        return positions;
    }
}
