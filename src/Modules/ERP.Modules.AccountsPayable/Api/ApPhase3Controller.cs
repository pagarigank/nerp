// <copyright file="ApPhase3Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Api;

#pragma warning disable S6960 // Controller intentionally groups related Phase-3 endpoint families; split-out would add churn without benefit.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap")]
public class ApPhase3Controller : ControllerBase
{
    private readonly ApDbContext _context;
    private readonly IApPhase3Service _service;

    public ApPhase3Controller(ApDbContext context, IApPhase3Service service)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    // --- Duplicate Invoice Detection ---
    [HttpPost("duplicate-invoice-check")]
    public async Task<ActionResult<DuplicateInvoiceCheckDto>> CheckDuplicate(
        [FromBody] CheckDuplicateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CheckDuplicateInvoiceAsync(
            request.CompanyId, request.VendorId, request.InvoiceNumber, request.Amount, request.LookbackDays, cancellationToken);
        return Ok(MapDuplicate(result));
    }

    [HttpGet("duplicate-invoice-checks")]
    public async Task<ActionResult<IReadOnlyList<DuplicateInvoiceCheckDto>>> GetDuplicateChecks(
        [FromQuery] Guid companyId, [FromQuery] bool? onlyDuplicates, CancellationToken cancellationToken)
    {
        var query = _context.DuplicateInvoiceChecks.Where(x => x.CompanyId == companyId);
        if (onlyDuplicates == true)
        {
            query = query.Where(x => x.IsDuplicate);
        }

        var items = await query.OrderByDescending(x => x.CheckedOn).ToListAsync(cancellationToken);
        return Ok(items.Select(MapDuplicate).ToList());
    }

    // --- W-9 / TIN ---
    [HttpPost("vendors/{vendorId:guid}/w9")]
    public async Task<ActionResult<VendorW9Dto>> CaptureW9(
        Guid vendorId, [FromBody] CaptureW9Request request, CancellationToken cancellationToken)
    {
        var result = await _service.CaptureW9Async(
            vendorId, request.TaxId, request.LegalName, request.TinVerified, request.TinMatchStatus, cancellationToken);
        return Ok(MapW9(result));
    }

    [HttpGet("vendors/{vendorId:guid}/w9")]
    public async Task<ActionResult<IReadOnlyList<VendorW9Dto>>> GetW9(
        Guid vendorId, CancellationToken cancellationToken)
    {
        var items = await _context.VendorW9Records.Where(x => x.VendorId == vendorId).OrderByDescending(x => x.CapturedOn).ToListAsync(cancellationToken);
        return Ok(items.Select(MapW9).ToList());
    }

    // --- Bank Account Pre-Note Verification ---
    [HttpPost("bank-verifications")]
    public async Task<ActionResult<VendorBankVerificationDto>> VerifyBank(
        [FromBody] VerifyBankRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.VerifyBankAccountAsync(
            request.VendorBankAccountId, request.RoutingNumber, request.AccountNumber, cancellationToken);
        return Ok(MapBank(result));
    }

    [HttpPost("bank-verifications/{id:guid}/approve")]
    public async Task<ActionResult<VendorBankVerificationDto>> ApproveBank(
        Guid id, [FromBody] BankDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ApproveBankVerificationAsync(id, request.Notes, cancellationToken);
        return Ok(MapBank(result));
    }

    [HttpPost("bank-verifications/{id:guid}/reject")]
    public async Task<ActionResult<VendorBankVerificationDto>> RejectBank(
        Guid id, [FromBody] BankDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RejectBankVerificationAsync(id, request.Notes ?? string.Empty, cancellationToken);
        return Ok(MapBank(result));
    }

    [HttpGet("bank-verifications")]
    public async Task<ActionResult<IReadOnlyList<VendorBankVerificationDto>>> GetBankVerifications(
        [FromQuery] Guid? vendorBankAccountId, CancellationToken cancellationToken)
    {
        var query = _context.VendorBankVerifications.AsQueryable();
        if (vendorBankAccountId.HasValue)
        {
            query = query.Where(x => x.VendorBankAccountId == vendorBankAccountId.Value);
        }

        var items = await query.OrderByDescending(x => x.CreatedOn).ToListAsync(cancellationToken);
        return Ok(items.Select(MapBank).ToList());
    }

    // --- Cash Discount Capture ---
    [HttpPost("cash-discounts")]
    public async Task<ActionResult<CashDiscountCaptureDto>> CaptureDiscount(
        [FromBody] CaptureDiscountRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CaptureCashDiscountAsync(
            request.VoucherId, request.VendorId, request.InvoiceAmount, request.DiscountAvailable, request.DiscountTaken, request.DiscountLost, cancellationToken);
        return Ok(MapDiscount(result));
    }

    [HttpGet("cash-discounts")]
    public async Task<ActionResult<IReadOnlyList<CashDiscountCaptureDto>>> GetDiscounts(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var vendorIds = await _context.Vouchers
            .Where(v => v.VoucherBatch != null && v.VoucherBatch.CompanyId == companyId)
            .Select(v => v.VendorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var items = await _context.CashDiscountCaptures
            .Where(x => vendorIds.Contains(x.VendorId))
            .OrderByDescending(x => x.CapturedOn)
            .ToListAsync(cancellationToken);
        return Ok(items.Select(MapDiscount).ToList());
    }

    [HttpGet("cash-discounts/lost-summary")]
    public async Task<ActionResult<LostDiscountSummaryDto>> GetLostDiscountSummary(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var vendorIds = await _context.Vouchers
            .Where(v => v.VoucherBatch != null && v.VoucherBatch.CompanyId == companyId)
            .Select(v => v.VendorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var items = await _context.CashDiscountCaptures
            .Where(x => vendorIds.Contains(x.VendorId))
            .ToListAsync(cancellationToken);

        return Ok(new LostDiscountSummaryDto(
            companyId,
            items.Sum(x => x.DiscountAvailable),
            items.Sum(x => x.DiscountTaken),
            items.Sum(x => x.DiscountLostAmount),
            items.Count(x => x.DiscountLost)));
    }

    // --- Stale Check Escheatment ---
    [HttpPost("escheatment/flag")]
    public async Task<ActionResult<IReadOnlyList<StaleCheckEscheatmentDto>>> FlagStaleChecks(
        [FromBody] FlagStaleChecksRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.FlagStaleChecksAsync(request.CompanyId, request.StatutoryDays, cancellationToken);
        return Ok(result.Select(MapEscheat).ToList());
    }

    [HttpGet("escheatment")]
    public async Task<ActionResult<IReadOnlyList<StaleCheckEscheatmentDto>>> GetEscheatment(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var items = await _context.StaleCheckEscheatments
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync(cancellationToken);
        return Ok(items.Select(MapEscheat).ToList());
    }

    [HttpPost("escheatment/{id:guid}/report")]
    public async Task<ActionResult<StaleCheckEscheatmentDto>> ReportEscheat(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.StaleCheckEscheatments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Escheatment {id} not found.");
        item.Report();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapEscheat(item));
    }

    // --- GR/IR Accrual Reversal ---
    [HttpPost("grir-accruals")]
    public async Task<ActionResult<GrirAccrualDto>> CreateAccrual(
        [FromBody] CreateGrirAccrualRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateGrirAccrualAsync(
            request.CompanyId, request.VendorId, request.PurchaseOrderId, request.ReceiptId, request.AccrualAmount, request.FiscalPeriodId, cancellationToken);
        return Ok(MapAccrual(result));
    }

    [HttpPost("grir-accruals/{id:guid}/reverse")]
    public async Task<ActionResult<GrirAccrualDto>> ReverseAccrual(
        Guid id, [FromBody] ReverseGrirAccrualRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ReverseGrirAccrualAsync(id, request.FiscalPeriodId, cancellationToken);
        return Ok(MapAccrual(result));
    }

    [HttpGet("grir-accruals")]
    public async Task<ActionResult<IReadOnlyList<GrirAccrualDto>>> GetAccruals(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var items = await _context.GrirAccruals.Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreatedOn).ToListAsync(cancellationToken);
        return Ok(items.Select(MapAccrual).ToList());
    }

    // --- Vendor Statement Reconciliation ---
    [HttpPost("vendor-statements")]
    public async Task<ActionResult<VendorStatementDto>> CreateStatement(
        [FromBody] CreateVendorStatementRequest request, CancellationToken cancellationToken)
    {
        var lines = request.Lines
            .Select(l => (l.Reference, l.StatementAmount, l.BookAmount, l.IsDisputed, l.Note))
            .ToList();
        var result = await _service.CreateVendorStatementAsync(
            request.CompanyId, request.VendorId, request.StatementNumber, request.StatementDate, request.StatementTotal, lines, cancellationToken);
        return Ok(MapStatement(result));
    }

    [HttpGet("vendor-statements")]
    public async Task<ActionResult<IReadOnlyList<VendorStatementDto>>> GetStatements(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var items = await _context.VendorStatements.Where(x => x.CompanyId == companyId).OrderByDescending(x => x.StatementDate).ToListAsync(cancellationToken);
        return Ok(items.Select(MapStatement).ToList());
    }

    [HttpPost("vendor-statements/{id:guid}/close")]
    public async Task<ActionResult<VendorStatementDto>> CloseStatement(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.VendorStatements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Statement {id} not found.");
        item.Close();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapStatement(item));
    }

    // --- 1099 NEC vs MISC Classification ---
    [HttpPost("1099/classify")]
    public async Task<ActionResult<Ap1099ClassificationDto>> Classify(
        [FromBody] Classify1099Request request, CancellationToken cancellationToken)
    {
        var result = await _service.Classify1099Async(request.VendorId, request.FormType, request.TaxYear, cancellationToken);
        return Ok(MapClassification(result));
    }

    [HttpGet("1099/classifications")]
    public async Task<ActionResult<IReadOnlyList<Ap1099ClassificationDto>>> GetClassifications(
        [FromQuery] Guid? vendorId, [FromQuery] int? taxYear, CancellationToken cancellationToken)
    {
        var query = _context.Ap1099Classifications.AsQueryable();
        if (vendorId.HasValue)
        {
            query = query.Where(x => x.VendorId == vendorId.Value);
        }

        if (taxYear.HasValue)
        {
            query = query.Where(x => x.TaxYear == taxYear.Value);
        }

        var items = await query.OrderByDescending(x => x.TaxYear).ToListAsync(cancellationToken);
        return Ok(items.Select(MapClassification).ToList());
    }

    // --- 4-Way Match (PO <-> Receipt <-> Invoice <-> Inspection) ---
    [HttpPost("four-way-match/validate")]
    public ActionResult<FourWayMatchResult> ValidateFourWay(
        [FromBody] FourWayMatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var line in request.Lines)
        {
            if (line.InvoicedQuantity > line.ReceivedQuantity)
            {
                var pct = line.ReceivedQuantity == 0 ? 1 : (line.InvoicedQuantity - line.ReceivedQuantity) / line.ReceivedQuantity;
                if (pct > request.TolerancePercent)
                {
                    errors.Add($"Line '{line.ItemCode}': Invoice qty ({line.InvoicedQuantity}) exceeds received qty ({line.ReceivedQuantity}).");
                }
            }

            if (line.InspectedQuantity.HasValue && line.InspectedQuantity.Value < line.InvoicedQuantity)
            {
                errors.Add($"Line '{line.ItemCode}': Inspected qty ({line.InspectedQuantity}) is less than invoiced qty ({line.InvoicedQuantity}) — quality hold.");
            }
        }

        return Ok(new FourWayMatchResult(
            errors.Count == 0,
            errors,
            warnings,
            request.TolerancePercent));
    }

    // --- Mappers ---
    private static DuplicateInvoiceCheckDto MapDuplicate(DuplicateInvoiceCheck x) => new(
        x.Id, x.CompanyId, x.VendorId, x.InvoiceNumber, x.Amount, x.ConflictingVoucherId, x.IsDuplicate, x.CheckedOn);

    private static VendorW9Dto MapW9(VendorW9 x) => new(
        x.Id, x.VendorId, x.TaxId, x.LegalName, x.TinVerified, x.TinMatchStatus, x.CapturedOn);

    private static VendorBankVerificationDto MapBank(VendorBankVerification x) => new(
        x.Id, x.VendorBankAccountId, x.RoutingNumber, x.AccountNumber, x.Status.ToString(), x.Notes, x.CreatedOn);

    private static CashDiscountCaptureDto MapDiscount(CashDiscountCapture x) => new(
        x.Id, x.VoucherId, x.VendorId, x.InvoiceAmount, x.DiscountAvailable, x.DiscountTaken, x.DiscountLostAmount, x.DiscountLost, x.CapturedOn);

    private static StaleCheckEscheatmentDto MapEscheat(StaleCheckEscheatment x) => new(
        x.Id, x.PaymentId, x.VendorId, x.Amount, x.IssuedDate, x.StatutoryDays, x.Status.ToString(), x.ReportedOn);

    private static GrirAccrualDto MapAccrual(GrirAccrual x) => new(
        x.Id, x.CompanyId, x.VendorId, x.PurchaseOrderId, x.ReceiptId, x.AccrualAmount, x.FiscalPeriodId, x.ReversedByAccrualId, x.CreatedOn);

    private static VendorStatementDto MapStatement(VendorStatement x) => new(
        x.Id, x.CompanyId, x.VendorId, x.StatementNumber, x.StatementDate, x.StatementTotal, x.BookTotal, x.DisputedTotal, x.Status.ToString(), x.Lines.Select(l => new VendorStatementLineDto(l.Id, l.Reference, l.StatementAmount, l.BookAmount, l.Difference, l.IsDisputed, l.Note)).ToList());

    private static Ap1099ClassificationDto MapClassification(Ap1099Classification x) => new(
        x.Id, x.VendorId, x.FormType.ToString(), x.TaxYear);
}

// --- Request / DTO records ---
public record CheckDuplicateRequest(Guid CompanyId, Guid VendorId, string InvoiceNumber, decimal Amount, int LookbackDays);
public record DuplicateInvoiceCheckDto(Guid Id, Guid CompanyId, Guid VendorId, string InvoiceNumber, decimal Amount, Guid? ConflictingVoucherId, bool IsDuplicate, DateTimeOffset CheckedOn);

public record CaptureW9Request(string TaxId, string LegalName, bool TinVerified, string? TinMatchStatus);
public record VendorW9Dto(Guid Id, Guid VendorId, string TaxId, string LegalName, bool TinVerified, string? TinMatchStatus, DateTimeOffset CapturedOn);

public record VerifyBankRequest(Guid VendorBankAccountId, string RoutingNumber, string AccountNumber);
public record BankDecisionRequest(string? Notes);
public record VendorBankVerificationDto(Guid Id, Guid VendorBankAccountId, string RoutingNumber, string AccountNumber, string Status, string? Notes, DateTimeOffset CreatedOn);

public record CaptureDiscountRequest(Guid VoucherId, Guid VendorId, decimal InvoiceAmount, decimal DiscountAvailable, decimal DiscountTaken, bool DiscountLost);
public record CashDiscountCaptureDto(Guid Id, Guid VoucherId, Guid VendorId, decimal InvoiceAmount, decimal DiscountAvailable, decimal DiscountTaken, decimal DiscountLostAmount, bool DiscountLost, DateTimeOffset CapturedOn);
public record LostDiscountSummaryDto(Guid CompanyId, decimal TotalAvailable, decimal TotalTaken, decimal TotalLost, int LostCount);

public record FlagStaleChecksRequest(Guid CompanyId, int StatutoryDays);
public record StaleCheckEscheatmentDto(Guid Id, Guid PaymentId, Guid VendorId, decimal Amount, DateTimeOffset IssuedDate, int StatutoryDays, string Status, DateTimeOffset? ReportedOn);

public record CreateGrirAccrualRequest(Guid CompanyId, Guid VendorId, Guid? PurchaseOrderId, Guid? ReceiptId, decimal AccrualAmount, Guid FiscalPeriodId);
public record ReverseGrirAccrualRequest(Guid FiscalPeriodId);
public record GrirAccrualDto(Guid Id, Guid CompanyId, Guid VendorId, Guid? PurchaseOrderId, Guid? ReceiptId, decimal AccrualAmount, Guid FiscalPeriodId, Guid? ReversedByAccrualId, DateTimeOffset CreatedOn);

public record CreateVendorStatementRequest(Guid CompanyId, Guid VendorId, string StatementNumber, DateTimeOffset StatementDate, decimal StatementTotal, IReadOnlyList<CreateVendorStatementLineRequest> Lines);
public record CreateVendorStatementLineRequest(string Reference, decimal StatementAmount, decimal BookAmount, bool IsDisputed, string? Note);
public record VendorStatementDto(Guid Id, Guid CompanyId, Guid VendorId, string StatementNumber, DateTimeOffset StatementDate, decimal StatementTotal, decimal BookTotal, decimal DisputedTotal, string Status, IReadOnlyList<VendorStatementLineDto> Lines);
public record VendorStatementLineDto(Guid Id, string Reference, decimal StatementAmount, decimal BookAmount, decimal Difference, bool IsDisputed, string? Note);

public record Classify1099Request(Guid VendorId, int FormType, int TaxYear);
public record Ap1099ClassificationDto(Guid Id, Guid VendorId, string FormType, int TaxYear);

public record FourWayMatchLineDto(string ItemCode, decimal OrderedQuantity, decimal ReceivedQuantity, decimal InvoicedQuantity, decimal? InspectedQuantity, decimal UnitPrice, decimal ExtendedAmount);
public record FourWayMatchRequest(Guid CompanyId, string VendorId, string InvoiceNumber, IReadOnlyList<FourWayMatchLineDto> Lines, decimal InvoiceTotal, decimal TolerancePercent = 0.05m);
public record FourWayMatchResult(bool IsValid, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings, decimal TolerancePercent);
