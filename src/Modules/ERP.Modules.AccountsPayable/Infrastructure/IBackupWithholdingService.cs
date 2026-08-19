// <copyright file="IBackupWithholdingService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IBackupWithholdingService
{
    Task<BackupWithholdingResult> CalculateWithholdingAsync(
        Guid vendorId,
        decimal paymentAmount,
        CancellationToken cancellationToken = default);
}

public record BackupWithholdingResult(
    Guid VendorId,
    bool IsSubjectToWithholding,
    decimal WithholdingRate,
    decimal WithholdingAmount,
    decimal NetPaymentAmount);
