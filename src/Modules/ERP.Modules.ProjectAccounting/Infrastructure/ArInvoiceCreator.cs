// <copyright file="ArInvoiceCreator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure;

/// <summary>
/// Creates an Accounts Receivable invoice from a project billing event (billing-to-AR integration, §7.2).
/// PA holds a one-way reference to AR so it can post the invoice directly; AR's own
/// dual-posting to GL fires when the invoice batch is released/posted.
/// </summary>
public class ArInvoiceCreator
{
    private readonly ArDbContext _ar;
    private readonly PlatformDbContext _platform;

    public ArInvoiceCreator(ArDbContext ar, PlatformDbContext platform)
    {
        _ar = ar;
        _platform = platform;
    }

    /// <summary>Creates (and releases) an AR invoice for the given project billing amount.</summary>
    /// <param name="project">The project being billed.</param>
    /// <param name="invoiceNumber">The project billing invoice number.</param>
    /// <param name="amount">The net billed amount.</param>
    /// <param name="description">The invoice description.</param>
    /// <param name="invoiceDate">The billing date.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created AR invoice identifier.</returns>
    public async Task<Guid> CreateProjectInvoiceAsync(
        Project project,
        string invoiceNumber,
        decimal amount,
        string description,
        DateTimeOffset invoiceDate,
        CancellationToken ct)
    {
        if (project.CustomerId is null)
            throw new InvalidOperationException("Project has no customer; cannot create an AR invoice.");

        var fiscalPeriod = await _platform.FiscalPeriods
            .OrderByDescending(f => f.StartDate)
            .FirstOrDefaultAsync(f => f.CompanyId == project.CompanyId && f.StartDate <= invoiceDate && f.EndDate >= invoiceDate, ct)
            ?? await _platform.FiscalPeriods.OrderBy(f => f.StartDate).FirstOrDefaultAsync(f => f.CompanyId == project.CompanyId, ct);

        if (fiscalPeriod is null)
            throw new InvalidOperationException("No fiscal period available for the billing date.");

        var batch = new InvoiceBatch(
            project.CompanyId,
            $"PROJ-INV-{invoiceNumber}",
            $"Project billing: {project.Name}",
            invoiceDate,
            fiscalPeriod.Id);

        var dueDate = invoiceDate.AddDays(30);
        var invoice = batch.AddInvoice(
            project.CustomerId.Value,
            invoiceNumber,
            invoiceDate,
            dueDate,
            description,
            null,
            project.Id,
            null);

        // Post the billed amount to a contract-revenue account (find-or-create 4100 per company).
        invoice.AddLine(
            accountId: await ResolveRevenueAccountAsync(project.CompanyId, ct),
            description: description,
            quantity: 1,
            unitPrice: amount,
            taxAmount: 0,
            discountAmount: 0);

        _ar.InvoiceBatches.Add(batch);
        await _ar.SaveChangesAsync(ct);

        batch.Release();
        await _ar.SaveChangesAsync(ct);

        return invoice.Id;
    }

    private async Task<Guid> ResolveRevenueAccountAsync(Guid companyId, CancellationToken ct)
    {
        // The AR invoice line posts to a contract-revenue account. Resolve the seeded
        // 4100 account for the company, creating it if the COA seed did not include one.
        var existing = await _platform.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountNumber == "4100", ct);
        if (existing is not null)
            return existing.Id;

        var revenue = new ERP.Modules.Platform.Domain.Entities.Account(
            companyId, "4100", "Contract Revenue", ERP.Modules.Platform.Domain.Entities.AccountType.Revenue, ERP.Modules.Platform.Domain.Entities.NormalBalance.Credit, true);
        _platform.Accounts.Add(revenue);
        await _platform.SaveChangesAsync(ct);
        return revenue.Id;
    }
}
