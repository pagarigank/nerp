// <copyright file="Form1099Service.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text;
using ERP.Modules.AccountsPayable.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class Form1099Service : IForm1099Service
{
    private readonly ApDbContext _context;

    public Form1099Service(ApDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Form1099SummaryResult> Get1099SummaryAsync(
        Guid companyId,
        int taxYear,
        CancellationToken cancellationToken = default)
    {
        var startDate = new DateTimeOffset(taxYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(taxYear, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var payments = await _context.Payments
            .Where(p => p.CompanyId == companyId
                && p.Status == PaymentStatus.Issued
                && p.PaymentDate >= startDate
                && p.PaymentDate <= endDate)
            .Include(p => p.Lines)
            .ToListAsync(cancellationToken);

        var voucherIds = payments.SelectMany(p => p.Lines).Select(l => l.VoucherId).Distinct().ToList();
        var vouchers = await _context.Vouchers
            .Where(v => voucherIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        var vendorIds = vouchers.Select(v => v.VendorId).Distinct().ToList();
        var vendors = await _context.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        var vendorMap = vendors.ToDictionary(v => v.Id);
        var voucherMap = vouchers.ToDictionary(v => v.Id);

        var vendorSummaries = new Dictionary<Guid, Form1099VendorSummary>();

        foreach (var payment in payments)
        {
            foreach (var line in payment.Lines)
            {
                if (!voucherMap.TryGetValue(line.VoucherId, out var voucher))
                    continue;

                if (!vendorMap.TryGetValue(voucher.VendorId, out var vendor))
                    continue;

                if (vendor.Form1099Category == null || vendor.Form1099Category == Vendor1099Category.None)
                    continue;

                var category = vendor.Form1099Category.Value;

                if (vendorSummaries.TryGetValue(vendor.Id, out var existing))
                {
                    vendorSummaries[vendor.Id] = existing with
                    {
                        TotalPayments = existing.TotalPayments + line.AppliedAmount,
                        BackupWithholdingAmount = existing.BackupWithholdingAmount + voucher.BackupWithholdingAmount,
                    };
                }
                else
                {
                    vendorSummaries[vendor.Id] = new Form1099VendorSummary(
                        vendor.Id,
                        vendor.VendorId,
                        vendor.Name,
                        vendor.LegalName,
                        vendor.TaxId,
                        category,
                        line.AppliedAmount,
                        voucher.BackupWithholdingAmount);
                }
            }
        }

        var summaries = vendorSummaries.Values.OrderBy(s => s.Name).ToList();
        var totalPayments = summaries.Sum(s => s.TotalPayments);
        var totalWithholding = summaries.Sum(s => s.BackupWithholdingAmount);

        return new Form1099SummaryResult(companyId, taxYear, summaries, totalPayments, totalWithholding);
    }

    public async Task<string> GenerateEfileContentAsync(
        Guid companyId,
        int taxYear,
        CancellationToken cancellationToken = default)
    {
        var summary = await Get1099SummaryAsync(companyId, taxYear, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("TaxYear,VendorCode,VendorName,LegalName,TaxId,1099Category,TotalPayments,BackupWithholding");

        foreach (var vendor in summary.Vendors)
        {
            var categoryCode = vendor.Category switch
            {
                Vendor1099Category.IndependentContractor => "NEC",
                Vendor1099Category.NonEmployeeCompensation => "NEC",
                Vendor1099Category.Rent => "RENT",
                Vendor1099Category.Royalties => "ROY",
                Vendor1099Category.MedicalAndHealth => "MED",
                Vendor1099Category.Attorney => "ATT",
                _ => "OTH",
            };

            sb.AppendLine(string.Join(",",
                taxYear.ToString(CultureInfo.InvariantCulture),
                CsvEscape(vendor.VendorIdCode),
                CsvEscape(vendor.Name),
                CsvEscape(vendor.LegalName ?? string.Empty),
                CsvEscape(vendor.TaxId ?? string.Empty),
                categoryCode,
                vendor.TotalPayments.ToString("F2", CultureInfo.InvariantCulture),
                vendor.BackupWithholdingAmount.ToString("F2", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

        return value;
    }
}
