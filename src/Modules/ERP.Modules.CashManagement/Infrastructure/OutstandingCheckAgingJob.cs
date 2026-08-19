// <copyright file="OutstandingCheckAgingJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public record OutstandingCheckAgingBucket(
    string Bucket,
    decimal Amount,
    int CheckCount);

public record OutstandingCheckAgingReport(
    Guid BankAccountId,
    string AccountName,
    DateTimeOffset AsOfDate,
    IReadOnlyList<OutstandingCheckAgingBucket> Buckets);

public interface IOutstandingCheckAgingJob
{
    Task<OutstandingCheckAgingReport> RunAsync(
        Guid companyId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);
}

public class OutstandingCheckAgingJob : IOutstandingCheckAgingJob
{
    private readonly CashDbContext _context;
    private readonly IPositivePayService _positivePayService;

    public OutstandingCheckAgingJob(CashDbContext context, IPositivePayService positivePayService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _positivePayService = positivePayService ?? throw new ArgumentNullException(nameof(positivePayService));
    }

    public async Task<OutstandingCheckAgingReport> RunAsync(
        Guid companyId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == bankAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        var asOf = DateTimeOffset.UtcNow;
        var checks = await _positivePayService.GetOutstandingChecksAsync(companyId, bankAccountId, asOf, cancellationToken);

        var buckets = new List<OutstandingCheckAgingBucket>
        {
            new("0-30 Days", 0, 0),
            new("31-60 Days", 0, 0),
            new("61-90 Days", 0, 0),
            new("90+ Days", 0, 0),
        };

        foreach (var check in checks)
        {
            var age = (asOf.Date - check.Date.Date).Days;
            var bucket = age switch
            {
                <= 30 => buckets[0],
                <= 60 => buckets[1],
                <= 90 => buckets[2],
                _ => buckets[3],
            };

            buckets[buckets.IndexOf(bucket)] = bucket with
            {
                Amount = bucket.Amount + check.Amount,
                CheckCount = bucket.CheckCount + 1,
            };
        }

        return new OutstandingCheckAgingReport(account.Id, account.AccountName, asOf, buckets);
    }
}
