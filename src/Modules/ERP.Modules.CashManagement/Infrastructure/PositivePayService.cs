// <copyright file="PositivePayService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public interface IPositivePayService
{
    Task<IReadOnlyList<PositivePayLine>> GetOutstandingChecksAsync(
        Guid companyId,
        Guid bankAccountId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default);

    Task<string> ExportCsvAsync(
        Guid companyId,
        Guid bankAccountId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default);
}

public record PositivePayLine(
    string AccountNumber,
    string CheckNumber,
    decimal Amount,
    DateTimeOffset Date,
    string? Payee);

public class PositivePayService : IPositivePayService
{
    private readonly CashDbContext _context;
    private readonly ApDbContext _apContext;

    public PositivePayService(CashDbContext context, ApDbContext apContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
    }

    public async Task<IReadOnlyList<PositivePayLine>> GetOutstandingChecksAsync(
        Guid companyId,
        Guid bankAccountId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default)
    {
        var bankAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == bankAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        var payments = await _apContext.Payments
            .Where(p => p.CompanyId == companyId
                && p.BankAccountId == bankAccountId
                && p.PaymentMethod == PaymentMethod.Check
                && p.Status == PaymentStatus.Issued
                && p.PaymentDate <= asOfDate)
            .ToListAsync(cancellationToken);

        var vendorIds = payments.Select(p => p.VendorId).Distinct().ToList();
        var vendors = await _apContext.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name, cancellationToken);

        return payments
            .Select(p => new PositivePayLine(
                bankAccount.AccountNumber,
                p.PaymentReference,
                p.TotalAmount,
                p.PaymentDate,
                vendors.TryGetValue(p.VendorId, out var name) ? name : null))
            .ToList();
    }

    public async Task<string> ExportCsvAsync(
        Guid companyId,
        Guid bankAccountId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default)
    {
        var lines = await GetOutstandingChecksAsync(companyId, bankAccountId, asOfDate, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("AccountNumber,CheckNumber,Amount,Date,Payee");

        foreach (var line in lines)
        {
            builder.AppendLine(
                string.Join(',',
                    Escape(line.AccountNumber),
                    Escape(line.CheckNumber),
                    line.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    line.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Escape(line.Payee ?? string.Empty)));
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
