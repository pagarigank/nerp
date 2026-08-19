// <copyright file="ApPhase3Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

/// <summary>
/// Detects duplicate vendor invoices (same vendor + invoice number + amount within a lookback window).
/// </summary>
public class DuplicateInvoiceCheck : Entity
{
    protected DuplicateInvoiceCheck() { }

    public DuplicateInvoiceCheck(
        Guid companyId,
        Guid vendorId,
        string invoiceNumber,
        decimal amount,
        Guid? conflictingVoucherId = null,
        bool isDuplicate = false)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        VendorId = vendorId;
        InvoiceNumber = invoiceNumber ?? throw new ArgumentNullException(nameof(invoiceNumber));
        Amount = amount;
        ConflictingVoucherId = conflictingVoucherId;
        IsDuplicate = isDuplicate;
        CheckedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid VendorId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public Guid? ConflictingVoucherId { get; private set; }
    public bool IsDuplicate { get; private set; }
    public DateTimeOffset CheckedOn { get; private set; }
}

/// <summary>
/// Vendor W-9 record with TIN and IRS TIN-match status for 1099 threshold tracking.
/// </summary>
public class VendorW9 : Entity
{
    protected VendorW9() { }

    public VendorW9(
        Guid vendorId,
        string taxId,
        string legalName,
        bool tinVerified,
        string? tinMatchStatus = null)
        : base(Guid.NewGuid())
    {
        VendorId = vendorId;
        TaxId = taxId ?? throw new ArgumentNullException(nameof(taxId));
        LegalName = legalName ?? throw new ArgumentNullException(nameof(legalName));
        TinVerified = tinVerified;
        TinMatchStatus = tinMatchStatus;
        CapturedOn = DateTimeOffset.UtcNow;
    }

    public Guid VendorId { get; private set; }
    public string TaxId { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public bool TinVerified { get; private set; }
    public string? TinMatchStatus { get; private set; }
    public DateTimeOffset CapturedOn { get; private set; }

    public void Verify(string matchStatus)
    {
        TinVerified = true;
        TinMatchStatus = matchStatus;
    }

    public void FlagMissingTin()
    {
        TinVerified = false;
        TinMatchStatus = "Missing TIN";
    }
}

/// <summary>
/// Vendor bank-account pre-note / ACH validation before first payment (NACHA rules).
/// </summary>
public class VendorBankVerification : Entity
{
    protected VendorBankVerification() { }

    public VendorBankVerification(Guid vendorBankAccountId, string routingNumber, string accountNumber)
        : base(Guid.NewGuid())
    {
        VendorBankAccountId = vendorBankAccountId;
        RoutingNumber = routingNumber ?? throw new ArgumentNullException(nameof(routingNumber));
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        Status = VerificationStatus.Pending;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public Guid VendorBankAccountId { get; private set; }
    public string RoutingNumber { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public VerificationStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    public void Approve(string? notes = null)
    {
        Status = VerificationStatus.Approved;
        Notes = notes;
    }

    public void Reject(string notes)
    {
        Status = VerificationStatus.Rejected;
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
    }
}

public enum VerificationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>
/// Captured cash discount (2/10 net 30) — available vs taken — and lost-discount reporting.
/// </summary>
public class CashDiscountCapture : Entity
{
    protected CashDiscountCapture() { }

    public CashDiscountCapture(
        Guid voucherId,
        Guid vendorId,
        decimal invoiceAmount,
        decimal discountAvailable,
        decimal discountTaken,
        bool discountLost)
        : base(Guid.NewGuid())
    {
        VoucherId = voucherId;
        VendorId = vendorId;
        InvoiceAmount = invoiceAmount;
        DiscountAvailable = discountAvailable;
        DiscountTaken = discountTaken;
        DiscountLost = discountLost;
        CapturedOn = DateTimeOffset.UtcNow;
    }

    public Guid VoucherId { get; private set; }
    public Guid VendorId { get; private set; }
    public decimal InvoiceAmount { get; private set; }
    public decimal DiscountAvailable { get; private set; }
    public decimal DiscountTaken { get; private set; }
    public decimal DiscountLostAmount => DiscountAvailable - DiscountTaken;
    public bool DiscountLost { get; private set; }
    public DateTimeOffset CapturedOn { get; private set; }
}

/// <summary>
/// Unclaimed property / stale-check escheatment workflow (uncashed AP checks past statutory period).
/// </summary>
public class StaleCheckEscheatment : Entity
{
    protected StaleCheckEscheatment() { }

    public StaleCheckEscheatment(
        Guid companyId,
        Guid paymentId,
        Guid vendorId,
        decimal amount,
        DateTimeOffset issuedDate,
        int statutoryDays)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        PaymentId = paymentId;
        VendorId = vendorId;
        Amount = amount;
        IssuedDate = issuedDate;
        StatutoryDays = statutoryDays;
        Status = EscheatmentStatus.Flagged;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }

    public Guid PaymentId { get; private set; }
    public Guid VendorId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset IssuedDate { get; private set; }
    public int StatutoryDays { get; private set; }
    public EscheatmentStatus Status { get; private set; }
    public DateTimeOffset? ReportedOn { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    public void Report()
    {
        Status = EscheatmentStatus.Reported;
        ReportedOn = DateTimeOffset.UtcNow;
    }

    public void Reissue()
    {
        Status = EscheatmentStatus.Reissued;
    }
}

public enum EscheatmentStatus
{
    Flagged = 0,
    Reported = 1,
    Reissued = 2,
}

/// <summary>
/// Goods-received-not-invoiced (GR/IR) accrual at period close, reversed next period.
/// </summary>
public class GrirAccrual : Entity
{
    protected GrirAccrual() { }

    public GrirAccrual(
        Guid companyId,
        Guid vendorId,
        Guid? purchaseOrderId,
        Guid? receiptId,
        decimal accrualAmount,
        Guid fiscalPeriodId,
        Guid? reversedByAccrualId = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        VendorId = vendorId;
        PurchaseOrderId = purchaseOrderId;
        ReceiptId = receiptId;
        AccrualAmount = accrualAmount;
        FiscalPeriodId = fiscalPeriodId;
        ReversedByAccrualId = reversedByAccrualId;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }
    public Guid? ReceiptId { get; private set; }
    public decimal AccrualAmount { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public Guid? ReversedByAccrualId { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    public void SetReversal(Guid reversalId) => ReversedByAccrualId = reversalId;
}

/// <summary>
/// Vendor monthly statement reconciliation (import/compare against open vouchers; dispute tracking).
/// </summary>
public class VendorStatement : Entity
{
    private readonly List<VendorStatementLine> _lines = [];

    protected VendorStatement() { }

    public VendorStatement(
        Guid companyId,
        Guid vendorId,
        string statementNumber,
        DateTimeOffset statementDate,
        decimal statementTotal)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        VendorId = vendorId;
        StatementNumber = statementNumber ?? throw new ArgumentNullException(nameof(statementNumber));
        StatementDate = statementDate;
        StatementTotal = statementTotal;
        Status = VendorStatementStatus.Open;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid VendorId { get; private set; }
    public string StatementNumber { get; private set; } = string.Empty;
    public DateTimeOffset StatementDate { get; private set; }
    public decimal StatementTotal { get; private set; }
    public decimal BookTotal => _lines.Sum(l => l.BookAmount);
    public decimal DisputedTotal => _lines.Where(l => l.IsDisputed).Sum(l => l.Difference);
    public VendorStatementStatus Status { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    public IReadOnlyList<VendorStatementLine> Lines => _lines.AsReadOnly();

    public VendorStatementLine AddLine(string reference, decimal statementAmount, decimal bookAmount, bool isDisputed, string? note)
    {
        var line = new VendorStatementLine(Id, reference, statementAmount, bookAmount, isDisputed, note);
        _lines.Add(line);
        return line;
    }

    public void Close() => Status = VendorStatementStatus.Closed;

    public void Reopen() => Status = VendorStatementStatus.Open;
}

public enum VendorStatementStatus
{
    Open = 0,
    Closed = 1,
}

public class VendorStatementLine : Entity
{
    protected VendorStatementLine() { }

    public VendorStatementLine(
        Guid vendorStatementId,
        string reference,
        decimal statementAmount,
        decimal bookAmount,
        bool isDisputed,
        string? note)
        : base(Guid.NewGuid())
    {
        VendorStatementId = vendorStatementId;
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        StatementAmount = statementAmount;
        BookAmount = bookAmount;
        IsDisputed = isDisputed;
        Note = note;
    }

    public Guid VendorStatementId { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal StatementAmount { get; private set; }
    public decimal BookAmount { get; private set; }
    public decimal Difference => StatementAmount - BookAmount;
    public bool IsDisputed { get; private set; }
    public string? Note { get; private set; }
}

/// <summary>
/// 1099-NEC vs 1099-MISC classification per IRS form type (extends the existing 1099 engine).
/// </summary>
public enum Form1099Type
{
    NEC = 0,
    MISC = 1,
}

public class Ap1099Classification : Entity
{
    protected Ap1099Classification() { }

    public Ap1099Classification(Guid vendorId, Form1099Type formType, int taxYear)
        : base(Guid.NewGuid())
    {
        VendorId = vendorId;
        FormType = formType;
        TaxYear = taxYear;
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public Guid VendorId { get; private set; }
    public Form1099Type FormType { get; private set; }
    public int TaxYear { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }
}
