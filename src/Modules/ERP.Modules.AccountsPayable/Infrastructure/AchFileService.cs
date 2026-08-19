// <copyright file="AchFileService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.AccountsPayable.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class AchFileService : IAchFileService
{
    private readonly ApDbContext _context;
    private readonly ILogger<AchFileService> _logger;

    public AchFileService(ApDbContext context, ILogger<AchFileService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AchFileGenerationResult> GenerateAchFileAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found.");

        return await GenerateAchContentAsync([payment], cancellationToken);
    }

    public async Task<AchFileGenerationResult> GenerateBatchAchFileAsync(
        IReadOnlyList<Guid> paymentIds,
        CancellationToken cancellationToken = default)
    {
        var payments = await _context.Payments
            .Include(p => p.Lines)
            .Where(p => paymentIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return await GenerateAchContentAsync(payments, cancellationToken);
    }

    public Task<string> GetAchFileContentAsync(string fileName, CancellationToken cancellationToken = default)
    {
        // In production, this would read from blob storage or file system
        // For now, return a placeholder indicating the file would be stored externally
        return Task.FromResult($"ACH file content for {fileName} - retrieved from storage");
    }

    private async Task<AchFileGenerationResult> GenerateAchContentAsync(
        IReadOnlyList<Payment> payments,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        var now = DateTimeOffset.UtcNow;
        var effectiveDate = now.AddDays(1);

        // NACHA file format header (Record Type 1)
        lines.Add($"101 000000000000000000{now:MMddyyyy}{now:HHmm}0940123456ERP ACH PAYMENTS     ");

        // Company/Batch header (Record Type 5)
        var entryCount = 0;
        var totalDebitAmount = 0m;

        var vendorIds = payments.Select(p => p.VendorId).Distinct().ToList();
        var vendors = await _context.Vendors
            .Include(v => v.BankAccounts)
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        foreach (var payment in payments)
        {
            if (!vendors.TryGetValue(payment.VendorId, out var vendor))
                continue;

            var bankAccounts = vendor.BankAccounts;
            if (bankAccounts == null || bankAccounts.Count == 0)
                continue;

            foreach (var bankAccount in vendor.BankAccounts)
            {
                entryCount++;

                // Batch header
                lines.Add($"5225{payment.CompanyId,-16}ERP ACH PAYMENTS     {(int)payment.PaymentMethod,-4}{effectiveDate:MMddyyyy}{effectiveDate:MMddyyyy}            {entryCount,6}{payment.Id:N}   ");

                // Entry detail (Record Type 6)
                var routing = bankAccount.RoutingNumber?.PadLeft(9, '0') ?? "000000000";
                var account = bankAccount.AccountNumber?.PadRight(17)[..17] ?? "00000000000000000";
                var amount = (payment.Lines?.Sum(l => l.AppliedAmount) ?? 0).ToString("0000000000", CultureInfo.InvariantCulture);
                lines.Add($"627{routing}{account} {amount} {vendor.VendorId,-15}{vendor.Name,-22}{bankAccount.Id:N}  ");

                // Addenda (optional)
                lines.Add($"710{payment.PaymentReference,-29}                                       ");

                totalDebitAmount += payment.Lines?.Sum(l => l.AppliedAmount) ?? 0;
            }
        }

        // File control (Record Type 9)
        var totalDebitStr = totalDebitAmount.ToString("00000000000000", CultureInfo.InvariantCulture);
        lines.Add($"9000001{entryCount,6}{totalDebitAmount * 100,10:F0}{totalDebitStr}                                    ");

        var content = string.Join("\r\n", lines);
        var fileName = $"ACH-{now:yyyyMMdd-HHmmss}.ach";

        _logger.LogInformation(
            "Generated ACH file {FileName} with {EntryCount} entries totaling {TotalAmount:C2}",
            fileName,
            entryCount,
            totalDebitAmount);

        return new AchFileGenerationResult(fileName, entryCount, totalDebitAmount, content);
    }
}
