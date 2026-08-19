// <copyright file="IForm1099Service.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IForm1099Service
{
    Task<Form1099SummaryResult> Get1099SummaryAsync(
        Guid companyId,
        int taxYear,
        CancellationToken cancellationToken = default);

    Task<string> GenerateEfileContentAsync(
        Guid companyId,
        int taxYear,
        CancellationToken cancellationToken = default);
}

public record Form1099VendorSummary(
    Guid VendorId,
    string VendorIdCode,
    string Name,
    string? LegalName,
    string? TaxId,
    Vendor1099Category Category,
    decimal TotalPayments,
    decimal BackupWithholdingAmount);

public record Form1099SummaryResult(
    Guid CompanyId,
    int TaxYear,
    IReadOnlyList<Form1099VendorSummary> Vendors,
    decimal TotalPayments,
    decimal TotalBackupWithholding);
