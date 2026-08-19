// <copyright file="NsfService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public interface INsfService
{
    Task<NsfRecord> ProcessAsync(
        Guid companyId,
        Guid bankAccountId,
        Guid cashReceiptId,
        string nsfNumber,
        decimal amount,
        DateTimeOffset returnedDate,
        string? bankReference,
        string? reason,
        decimal? nsfFeeAmount,
        string processedBy,
        CancellationToken cancellationToken = default);
}

public class NsfService : INsfService
{
    private readonly CashDbContext _context;
    private readonly ArDbContext _arContext;

    public NsfService(CashDbContext context, ArDbContext arContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
    }

    public async Task<NsfRecord> ProcessAsync(
        Guid companyId,
        Guid bankAccountId,
        Guid cashReceiptId,
        string nsfNumber,
        decimal amount,
        DateTimeOffset returnedDate,
        string? bankReference,
        string? reason,
        decimal? nsfFeeAmount,
        string processedBy,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _arContext.CashReceipts
            .Include(r => r.Applications)
            .FirstOrDefaultAsync(r => r.Id == cashReceiptId && !r.DeletedOn.HasValue, cancellationToken)
            ?? throw new InvalidOperationException($"Cash receipt {cashReceiptId} not found.");

        if (receipt.Status is CashReceiptStatus.Refunded)
            throw new InvalidOperationException("A refunded cash receipt cannot be processed as NSF.");

        var applications = receipt.Applications.ToList();
        var invoiceIds = applications.Select(a => a.InvoiceId).Distinct().ToList();
        var invoices = await _arContext.Invoices
            .Where(i => invoiceIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        // Reverse cash application: reopen AR invoices and remove applications.
        foreach (var application in applications)
        {
            var invoice = invoices.FirstOrDefault(i => i.Id == application.InvoiceId);
            if (invoice == null)
                continue;

            receipt.UnapplyInvoice(invoice, application);
            _arContext.CashReceiptApplications.Remove(application);
        }

        if (applications.Count > 0)
        {
            await _arContext.SaveChangesAsync(cancellationToken);
        }

        var nsf = new NsfRecord(
            companyId,
            bankAccountId,
            cashReceiptId,
            receipt.CustomerId,
            nsfNumber,
            amount,
            returnedDate,
            bankReference,
            reason);

        nsf.CreatedBy = processedBy;
        _context.NsfRecords.Add(nsf);

        if (nsfFeeAmount.HasValue && nsfFeeAmount.Value > 0)
        {
            var fee = new BankFee(
                companyId,
                bankAccountId,
                $"NSF-{nsfNumber}",
                BankFeeType.NsfFee,
                nsfFeeAmount.Value,
                returnedDate,
                $"NSF fee for returned item {bankReference}");
            fee.CreatedBy = processedBy;
            fee.Post();
            _context.BankFees.Add(fee);
            nsf.AttachNsfFee(nsfFeeAmount.Value, fee.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return nsf;
    }
}
