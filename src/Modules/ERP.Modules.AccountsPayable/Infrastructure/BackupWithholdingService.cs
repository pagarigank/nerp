// <copyright file="BackupWithholdingService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class BackupWithholdingService : IBackupWithholdingService
{
    private readonly ApDbContext _context;

    public BackupWithholdingService(ApDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<BackupWithholdingResult> CalculateWithholdingAsync(
        Guid vendorId,
        decimal paymentAmount,
        CancellationToken cancellationToken = default)
    {
        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken)
            ?? throw new InvalidOperationException($"Vendor {vendorId} not found.");

        if (!vendor.BackupWithholdingFlag || vendor.BackupWithholdingRate <= 0)
        {
            return new BackupWithholdingResult(
                vendorId,
                false,
                0m,
                0m,
                paymentAmount);
        }

        var rate = vendor.BackupWithholdingRate;
        var withholdingAmount = Math.Round(paymentAmount * rate, 2);
        var netAmount = paymentAmount - withholdingAmount;

        return new BackupWithholdingResult(
            vendorId,
            true,
            rate,
            withholdingAmount,
            netAmount);
    }
}
