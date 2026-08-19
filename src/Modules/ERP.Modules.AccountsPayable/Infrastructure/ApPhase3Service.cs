// <copyright file="ApPhase3Service.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IApPhase3Service
{
    Task<DuplicateInvoiceCheck> CheckDuplicateInvoiceAsync(
        Guid companyId, Guid vendorId, string invoiceNumber, decimal amount, int lookbackDays, CancellationToken cancellationToken);

    Task<VendorW9> CaptureW9Async(
        Guid vendorId, string taxId, string legalName, bool tinVerified, string? tinMatchStatus, CancellationToken cancellationToken);

    Task<VendorBankVerification> VerifyBankAccountAsync(
        Guid vendorBankAccountId, string routingNumber, string accountNumber, CancellationToken cancellationToken);

    Task<VendorBankVerification> ApproveBankVerificationAsync(Guid id, string? notes, CancellationToken cancellationToken);

    Task<VendorBankVerification> RejectBankVerificationAsync(Guid id, string notes, CancellationToken cancellationToken);

    Task<CashDiscountCapture> CaptureCashDiscountAsync(
        Guid voucherId, Guid vendorId, decimal invoiceAmount, decimal discountAvailable, decimal discountTaken, bool discountLost, CancellationToken cancellationToken);

    Task<IReadOnlyList<StaleCheckEscheatment>> FlagStaleChecksAsync(
        Guid companyId, int statutoryDays, CancellationToken cancellationToken);

    Task<GrirAccrual> CreateGrirAccrualAsync(
        Guid companyId, Guid vendorId, Guid? purchaseOrderId, Guid? receiptId, decimal accrualAmount, Guid fiscalPeriodId, CancellationToken cancellationToken);

    Task<GrirAccrual> ReverseGrirAccrualAsync(Guid accrualId, Guid fiscalPeriodId, CancellationToken cancellationToken);

    Task<VendorStatement> CreateVendorStatementAsync(
        Guid companyId,
        Guid vendorId,
        string statementNumber,
        DateTimeOffset statementDate,
        decimal statementTotal,
        IReadOnlyList<(string Reference, decimal StatementAmount, decimal BookAmount, bool IsDisputed, string? Note)> lines,
        CancellationToken cancellationToken);

    Task<Ap1099Classification> Classify1099Async(Guid vendorId, int formType, int taxYear, CancellationToken cancellationToken);
}

public class ApPhase3Service : IApPhase3Service
{
    private readonly ApDbContext _context;

    public ApPhase3Service(ApDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DuplicateInvoiceCheck> CheckDuplicateInvoiceAsync(
        Guid companyId, Guid vendorId, string invoiceNumber, decimal amount, int lookbackDays, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-lookbackDays);
        var conflict = await _context.Vouchers
            .Where(v => v.VendorId == vendorId
                && v.InvoiceNumber == invoiceNumber
                && v.TotalAmount == amount
                && v.VoucherBatch != null
                && v.VoucherBatch.Status == VoucherBatchStatus.Posted
                && v.VoucherBatch.PostingDate >= cutoff)
            .OrderBy(v => v.VoucherBatch!.PostingDate)
            .FirstOrDefaultAsync(cancellationToken);

        var check = new DuplicateInvoiceCheck(
            companyId,
            vendorId,
            invoiceNumber,
            amount,
            conflict?.Id,
            conflict != null);

        _context.DuplicateInvoiceChecks.Add(check);
        await _context.SaveChangesAsync(cancellationToken);
        return check;
    }

    public async Task<VendorW9> CaptureW9Async(
        Guid vendorId, string taxId, string legalName, bool tinVerified, string? tinMatchStatus, CancellationToken cancellationToken)
    {
        var w9 = new VendorW9(vendorId, taxId, legalName, tinVerified, tinMatchStatus);
        _context.VendorW9Records.Add(w9);
        await _context.SaveChangesAsync(cancellationToken);
        return w9;
    }

    public async Task<VendorBankVerification> VerifyBankAccountAsync(
        Guid vendorBankAccountId, string routingNumber, string accountNumber, CancellationToken cancellationToken)
    {
        var verification = new VendorBankVerification(vendorBankAccountId, routingNumber, accountNumber);
        _context.VendorBankVerifications.Add(verification);
        await _context.SaveChangesAsync(cancellationToken);
        return verification;
    }

    public async Task<VendorBankVerification> ApproveBankVerificationAsync(Guid id, string? notes, CancellationToken cancellationToken)
    {
        var v = await _context.VendorBankVerifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Verification {id} not found.");
        v.Approve(notes);
        await _context.SaveChangesAsync(cancellationToken);
        return v;
    }

    public async Task<VendorBankVerification> RejectBankVerificationAsync(Guid id, string notes, CancellationToken cancellationToken)
    {
        var v = await _context.VendorBankVerifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Verification {id} not found.");
        v.Reject(notes);
        await _context.SaveChangesAsync(cancellationToken);
        return v;
    }

    public async Task<CashDiscountCapture> CaptureCashDiscountAsync(
        Guid voucherId, Guid vendorId, decimal invoiceAmount, decimal discountAvailable, decimal discountTaken, bool discountLost, CancellationToken cancellationToken)
    {
        var capture = new CashDiscountCapture(voucherId, vendorId, invoiceAmount, discountAvailable, discountTaken, discountLost);
        _context.CashDiscountCaptures.Add(capture);
        await _context.SaveChangesAsync(cancellationToken);
        return capture;
    }

    public async Task<IReadOnlyList<StaleCheckEscheatment>> FlagStaleChecksAsync(
        Guid companyId, int statutoryDays, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-statutoryDays);
        var payments = await _context.Payments
            .Where(p => p.CompanyId == companyId
                && (p.Status == PaymentStatus.Issued || p.Status == PaymentStatus.Cleared)
                && p.PaymentDate <= cutoff)
            .ToListAsync(cancellationToken);

        var flagged = new List<StaleCheckEscheatment>();
        foreach (var p in payments)
        {
            var already = await _context.StaleCheckEscheatments
                .AnyAsync(e => e.PaymentId == p.Id, cancellationToken);
            if (already)
            {
                continue;
            }

            var escheat = new StaleCheckEscheatment(p.CompanyId, p.Id, p.VendorId, p.TotalAmount, p.PaymentDate, statutoryDays);
            _context.StaleCheckEscheatments.Add(escheat);
            flagged.Add(escheat);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return flagged;
    }

    public async Task<GrirAccrual> CreateGrirAccrualAsync(
        Guid companyId, Guid vendorId, Guid? purchaseOrderId, Guid? receiptId, decimal accrualAmount, Guid fiscalPeriodId, CancellationToken cancellationToken)
    {
        var accrual = new GrirAccrual(companyId, vendorId, purchaseOrderId, receiptId, accrualAmount, fiscalPeriodId);
        _context.GrirAccruals.Add(accrual);
        await _context.SaveChangesAsync(cancellationToken);
        return accrual;
    }

    public async Task<GrirAccrual> ReverseGrirAccrualAsync(Guid accrualId, Guid fiscalPeriodId, CancellationToken cancellationToken)
    {
        var original = await _context.GrirAccruals.FirstOrDefaultAsync(x => x.Id == accrualId, cancellationToken)
            ?? throw new InvalidOperationException($"Accrual {accrualId} not found.");

        var reversal = new GrirAccrual(
            original.CompanyId,
            original.VendorId,
            original.PurchaseOrderId,
            original.ReceiptId,
            -original.AccrualAmount,
            fiscalPeriodId,
            original.Id);

        _context.GrirAccruals.Add(reversal);
        original.SetReversal(reversal.Id);
        await _context.SaveChangesAsync(cancellationToken);
        return reversal;
    }

    public async Task<VendorStatement> CreateVendorStatementAsync(
        Guid companyId,
        Guid vendorId,
        string statementNumber,
        DateTimeOffset statementDate,
        decimal statementTotal,
        IReadOnlyList<(string Reference, decimal StatementAmount, decimal BookAmount, bool IsDisputed, string? Note)> lines,
        CancellationToken cancellationToken)
    {
        var statement = new VendorStatement(companyId, vendorId, statementNumber, statementDate, statementTotal);
        foreach (var line in lines)
        {
            statement.AddLine(line.Reference, line.StatementAmount, line.BookAmount, line.IsDisputed, line.Note);
        }

        _context.VendorStatements.Add(statement);
        await _context.SaveChangesAsync(cancellationToken);
        return statement;
    }

    public async Task<Ap1099Classification> Classify1099Async(Guid vendorId, int formType, int taxYear, CancellationToken cancellationToken)
    {
        var classification = new Ap1099Classification(vendorId, (Form1099Type)formType, taxYear);
        _context.Ap1099Classifications.Add(classification);
        await _context.SaveChangesAsync(cancellationToken);
        return classification;
    }
}
