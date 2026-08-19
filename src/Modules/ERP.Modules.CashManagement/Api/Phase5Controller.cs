// <copyright file="Phase5Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cash")]
public class Phase5Controller : ControllerBase
{
    private readonly CashDbContext _context;
    private readonly ArDbContext _arContext;
    private readonly ApDbContext _apContext;

    public Phase5Controller(CashDbContext context, ArDbContext arContext, ApDbContext apContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
    }

    // ---- Bank account → GL cash account mapping ----
    [HttpGet("bank-gl-mappings")]
    public async Task<ActionResult<IReadOnlyList<BankGlMappingDto>>> GetBankGlMappingsAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var mappings = await _context.BankGlMappings
            .Where(m => m.CompanyId == companyId && !m.DeletedOn.HasValue)
            .OrderBy(m => m.BankAccountId)
            .Select(m => new BankGlMappingDto(m.Id, m.CompanyId, m.BankAccountId, null, m.GlAccountId, m.IsDefault))
            .ToListAsync(cancellationToken);

        return Ok(mappings);
    }

    [HttpGet("bank-accounts/{bankAccountId:guid}/gl-mapping")]
    public async Task<ActionResult<BankGlMappingDto>> GetBankGlMappingAsync(
        Guid bankAccountId, CancellationToken cancellationToken)
    {
        var mapping = await _context.BankGlMappings
            .FirstOrDefaultAsync(m => m.BankAccountId == bankAccountId && !m.DeletedOn.HasValue, cancellationToken);

        if (mapping == null)
            return NotFound();

        return Ok(new BankGlMappingDto(mapping.Id, mapping.CompanyId, mapping.BankAccountId, null, mapping.GlAccountId, mapping.IsDefault));
    }

    [HttpPost("bank-gl-mappings")]
    public async Task<ActionResult<BankGlMappingDto>> CreateBankGlMappingAsync(
        CreateBankGlMappingRequest request, CancellationToken cancellationToken)
    {
        var existing = await _context.BankGlMappings
            .FirstOrDefaultAsync(m => m.BankAccountId == request.BankAccountId && !m.DeletedOn.HasValue, cancellationToken);

        if (existing != null)
            return Conflict("A GL mapping already exists for this bank account.");

        var mapping = new BankGlMapping(request.CompanyId, request.BankAccountId, request.GlAccountId, request.IsDefault);
        mapping.CreatedBy = "admin";
        _context.BankGlMappings.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            "GetBankGlMapping",
            new { bankAccountId = mapping.BankAccountId },
            new BankGlMappingDto(mapping.Id, mapping.CompanyId, mapping.BankAccountId, null, mapping.GlAccountId, mapping.IsDefault));
    }

    [HttpPut("bank-gl-mappings/{id:guid}")]
    public async Task<ActionResult<BankGlMappingDto>> UpdateBankGlMappingAsync(
        Guid id, UpdateBankGlMappingRequest request, CancellationToken cancellationToken)
    {
        var mapping = await _context.BankGlMappings
            .FirstOrDefaultAsync(m => m.Id == id && !m.DeletedOn.HasValue, cancellationToken);

        if (mapping == null)
            return NotFound();

        mapping.Update(request.GlAccountId, request.IsDefault);
        mapping.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new BankGlMappingDto(mapping.Id, mapping.CompanyId, mapping.BankAccountId, null, mapping.GlAccountId, mapping.IsDefault));
    }

    // ---- Lockbox / remote deposit capture import ----
    [HttpGet("lockbox-batches")]
    public async Task<ActionResult<IReadOnlyList<LockboxBatchDto>>> GetLockboxBatchesAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var batches = await _context.LockboxBatches
            .Where(b => b.CompanyId == companyId && !b.DeletedOn.HasValue)
            .Include(b => b.Items)
            .OrderByDescending(b => b.ImportedOn)
            .Select(b => MapLockbox(b))
            .ToListAsync(cancellationToken);

        return Ok(batches);
    }

    [HttpPost("lockbox-batches")]
    public async Task<ActionResult<LockboxBatchDto>> CreateLockboxBatchAsync(
        CreateLockboxBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = new LockboxBatch(request.CompanyId, request.BatchNumber, request.FileName, request.Format);
        batch.CreatedBy = "admin";

        foreach (var item in request.Items)
        {
            batch.AddItem(item.ReferenceNumber, item.CustomerId, item.CustomerName, item.Amount, item.RemittanceDate, item.InvoiceNumber);
        }

        _context.LockboxBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            "GetLockboxBatches",
            new { companyId = batch.CompanyId },
            MapLockbox(batch));
    }

    [HttpPost("lockbox-batches/{id:guid}/post")]
    public async Task<ActionResult<LockboxBatchDto>> PostLockboxBatchAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var batch = await _context.LockboxBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id && !b.DeletedOn.HasValue, cancellationToken);

        if (batch == null)
            return NotFound();

        foreach (var item in batch.Items)
        {
            if (item.CustomerId == null || string.IsNullOrWhiteSpace(item.InvoiceNumber))
                continue;

            var invoice = await _arContext.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.CustomerId == item.CustomerId.Value
                    && i.InvoiceNumber == item.InvoiceNumber
                    && (i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid), cancellationToken);

            if (invoice == null)
                continue;

            var receipt = new CashReceipt(
                batch.CompanyId,
                item.CustomerId.Value,
                "LBX-" + item.ReferenceNumber,
                item.Amount,
                item.RemittanceDate ?? DateTimeOffset.UtcNow,
                "Lockbox",
                "USD",
                item.ReferenceNumber);
            receipt.CreatedBy = "admin";

            var applicable = Math.Min(item.Amount, invoice.BalanceDue);
            if (applicable > 0)
            {
                receipt.ApplyToInvoice(invoice, applicable);
            }

            _arContext.CashReceipts.Add(receipt);
            item.MarkReceiptCreated();
        }

        batch.Post();
        await _context.SaveChangesAsync(cancellationToken);
        await _arContext.SaveChangesAsync(cancellationToken);

        return Ok(MapLockbox(batch));
    }

    // ---- Stale-dated check handling + escheatment ----
    [HttpGet("stale-check-escheatments")]
    public async Task<ActionResult<IReadOnlyList<StaleCheckEscheatmentDto>>> GetStaleCheckEscheatmentsAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.StaleCheckEscheatments
            .Where(x => x.CompanyId == companyId && !x.DeletedOn.HasValue)
            .OrderByDescending(x => x.IssueDate)
            .Select(x => new StaleCheckEscheatmentDto(x.Id, x.CompanyId, x.BankAccountId, x.CheckId, x.CheckNumber, x.Amount, x.IssueDate, x.Payee, x.State, x.Status.ToString(), x.EscheatedOn, x.ReissuedOn))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("stale-check-escheatments")]
    public async Task<ActionResult<StaleCheckEscheatmentDto>> CreateStaleCheckEscheatmentAsync(
        CreateStaleCheckEscheatmentRequest request, CancellationToken cancellationToken)
    {
        var row = new StaleCheckEscheatment(request.CompanyId, request.BankAccountId, request.CheckId, request.CheckNumber, request.Amount, request.IssueDate, request.Payee, request.State);
        row.CreatedBy = "admin";
        _context.StaleCheckEscheatments.Add(row);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            "GetStaleCheckEscheatments",
            new { companyId = row.CompanyId },
            new StaleCheckEscheatmentDto(row.Id, row.CompanyId, row.BankAccountId, row.CheckId, row.CheckNumber, row.Amount, row.IssueDate, row.Payee, row.State, row.Status.ToString(), row.EscheatedOn, row.ReissuedOn));
    }

    [HttpPost("stale-check-escheatments/{id:guid}/escheat")]
    public async Task<ActionResult<StaleCheckEscheatmentDto>> EscheatStaleCheckAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var row = await _context.StaleCheckEscheatments
            .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedOn.HasValue, cancellationToken);

        if (row == null)
            return NotFound();

        row.Escheat(DateTimeOffset.UtcNow);
        row.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new StaleCheckEscheatmentDto(row.Id, row.CompanyId, row.BankAccountId, row.CheckId, row.CheckNumber, row.Amount, row.IssueDate, row.Payee, row.State, row.Status.ToString(), row.EscheatedOn, row.ReissuedOn));
    }

    [HttpPost("stale-check-escheatments/{id:guid}/reissue")]
    public async Task<ActionResult<StaleCheckEscheatmentDto>> ReissueStaleCheckAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var row = await _context.StaleCheckEscheatments
            .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedOn.HasValue, cancellationToken);

        if (row == null)
            return NotFound();

        row.Reissue(DateTimeOffset.UtcNow);
        row.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new StaleCheckEscheatmentDto(row.Id, row.CompanyId, row.BankAccountId, row.CheckId, row.CheckNumber, row.Amount, row.IssueDate, row.Payee, row.State, row.Status.ToString(), row.EscheatedOn, row.ReissuedOn));
    }

    // ---- Positive pay exception handling ----
    [HttpGet("positive-pay-exceptions")]
    public async Task<ActionResult<IReadOnlyList<PositivePayExceptionDto>>> GetPositivePayExceptionsAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.PositivePayExceptions
            .Where(x => x.CompanyId == companyId && !x.DeletedOn.HasValue)
            .OrderByDescending(x => x.ReceivedOn)
            .Select(x => new PositivePayExceptionDto(x.Id, x.CompanyId, x.BankAccountId, x.CheckNumber, x.Amount, x.IssueDate, x.Decision.ToString(), x.DecisionReason, x.ReceivedOn, x.DecidedOn))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("positive-pay-exceptions")]
    public async Task<ActionResult<PositivePayExceptionDto>> CreatePositivePayExceptionAsync(
        CreatePositivePayExceptionRequest request, CancellationToken cancellationToken)
    {
        var row = new PositivePayDiscrepancy(request.CompanyId, request.BankAccountId, request.CheckNumber, request.Amount, request.IssueDate, request.DecisionReason);
        row.CreatedBy = "admin";
        _context.PositivePayExceptions.Add(row);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            "GetPositivePayExceptions",
            new { companyId = row.CompanyId },
            new PositivePayExceptionDto(row.Id, row.CompanyId, row.BankAccountId, row.CheckNumber, row.Amount, row.IssueDate, row.Decision.ToString(), row.DecisionReason, row.ReceivedOn, row.DecidedOn));
    }

    [HttpPost("positive-pay-exceptions/{id:guid}/decide")]
    public async Task<ActionResult<PositivePayExceptionDto>> DecidePositivePayAsync(
        Guid id, DecidePositivePayRequest request, CancellationToken cancellationToken)
    {
        var row = await _context.PositivePayExceptions
            .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedOn.HasValue, cancellationToken);

        if (row == null)
            return NotFound();

        if (!Enum.TryParse<PositivePayDecision>(request.Decision, out var decision))
            return BadRequest("Invalid decision. Use 'Pay' or 'NoPay'.");

        row.Decide(decision, request.DecisionReason);
        row.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new PositivePayExceptionDto(row.Id, row.CompanyId, row.BankAccountId, row.CheckNumber, row.Amount, row.IssueDate, row.Decision.ToString(), row.DecisionReason, row.ReceivedOn, row.DecidedOn));
    }

    // ---- Duplicate bank line detection ----
    [HttpGet("duplicate-lines")]
    public async Task<ActionResult<IReadOnlyList<BankDuplicateLineDto>>> GetDuplicateLinesAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var rows = await _context.BankDuplicateLines
            .Where(x => x.CompanyId == companyId && !x.DeletedOn.HasValue)
            .OrderByDescending(x => x.DetectedOn)
            .Select(x => new BankDuplicateLineDto(x.Id, x.CompanyId, x.BankAccountId, x.CheckNumber, x.Amount, x.TransactionDate, x.StatementLineId, x.StatementId, x.DetectedOn, x.Resolved))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("duplicate-lines/detect")]
    public async Task<ActionResult<BankDuplicateLineDto>> DetectDuplicateLinesAsync(
        [FromQuery] Guid companyId, [FromQuery] Guid bankAccountId, [FromQuery] Guid statementId, CancellationToken cancellationToken)
    {
        var lines = await _context.BankStatementLines
            .Where(l => l.BankStatementId == statementId)
            .ToListAsync(cancellationToken);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.CheckNumber))
                continue;

            var dup = await _context.BankStatementLines
                .AnyAsync(l => l.BankStatementId != statementId
                    && l.CheckNumber == line.CheckNumber
                    && l.Amount == line.Amount
                    && l.TransactionDate.Date == line.TransactionDate.Date, cancellationToken);

            if (!dup)
                continue;

            var existing = await _context.BankDuplicateLines
                .AnyAsync(d => d.StatementLineId == line.Id && !d.DeletedOn.HasValue, cancellationToken);

            if (existing)
                continue;

            var record = new BankDuplicateLine(companyId, bankAccountId, line.CheckNumber, line.Amount, line.TransactionDate, line.Id, statementId);
            record.CreatedBy = "admin";
            _context.BankDuplicateLines.Add(record);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.BankDuplicateLines
            .Where(x => x.CompanyId == companyId && x.StatementId == statementId && !x.DeletedOn.HasValue)
            .OrderByDescending(x => x.DetectedOn)
            .Select(x => new BankDuplicateLineDto(x.Id, x.CompanyId, x.BankAccountId, x.CheckNumber, x.Amount, x.TransactionDate, x.StatementLineId, x.StatementId, x.DetectedOn, x.Resolved))
            .ToListAsync(cancellationToken);

        return Ok(created);
    }

    [HttpPost("duplicate-lines/{id:guid}/resolve")]
    public async Task<ActionResult<BankDuplicateLineDto>> ResolveDuplicateLineAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var row = await _context.BankDuplicateLines
            .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedOn.HasValue, cancellationToken);

        if (row == null)
            return NotFound();

        row.MarkResolved();
        row.MarkModified("admin");
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new BankDuplicateLineDto(row.Id, row.CompanyId, row.BankAccountId, row.CheckNumber, row.Amount, row.TransactionDate, row.StatementLineId, row.StatementId, row.DetectedOn, row.Resolved));
    }

    // ---- Bank fee analysis report ----
    [HttpGet("fee-analysis")]
    public async Task<ActionResult<BankFeeAnalysisDto>> GetFeeAnalysisAsync(
        [FromQuery] Guid companyId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var existing = await _context.BankFeeAnalyses
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Year == year && a.Month == month && !a.DeletedOn.HasValue, cancellationToken);

        if (existing != null)
            return Ok(MapFeeAnalysis(existing));

        var fees = await _context.BankFees
            .Where(f => f.CompanyId == companyId && f.FeeDate.Year == year && f.FeeDate.Month == month && !f.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var analysis = new BankFeeAnalysis(companyId, year, month);
        analysis.CreatedBy = "admin";

        var grouped = fees
            .GroupBy(f => f.FeeType.ToString())
            .ToList();

        foreach (var g in grouped)
        {
            analysis.AddLine(g.Key, g.First().BankAccountId, g.Sum(f => f.Amount), g.Count());
        }

        _context.BankFeeAnalyses.Add(analysis);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapFeeAnalysis(analysis));
    }

    // ---- Cash position by forecast horizon ----
    [HttpGet("reports/cash-forecast-horizon")]
    public async Task<ActionResult<CashForecastHorizonResponse>> GetCashForecastHorizonAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var positions = await _context.BankAccounts
            .Where(a => a.CompanyId == companyId && !a.DeletedOn.HasValue)
            .SumAsync(a => a.CurrentBalance, cancellationToken);

        var openInvoiceIds = await _arContext.Invoices
            .Where(i => i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.PartiallyPaid)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        var lines = await _arContext.InvoiceLines
            .Where(l => l.InvoiceId != null && openInvoiceIds.Contains(l.InvoiceId.Value))
            .ToListAsync(cancellationToken);
        var applied = await _arContext.CashReceiptApplications
            .Where(a => openInvoiceIds.Contains(a.InvoiceId))
            .SumAsync(a => a.AppliedAmount, cancellationToken);
        var openReceivables = decimal.Round(lines.Sum(l => (l.Quantity * l.UnitPrice) + l.TaxAmount - l.DiscountAmount) - applied, 2, MidpointRounding.AwayFromZero);

        var payables = await _apContext.Vouchers
            .Where(v => v.VoucherBatchId != Guid.Empty && !v.SelectedForPayment)
            .ToListAsync(cancellationToken);

        var openPayables = payables.Sum(v => v.TotalAmount);

        var response = new CashForecastHorizonResponse(
            positions,
            positions - openPayables,
            positions - openPayables + openReceivables,
            openPayables,
            openReceivables,
            openPayables,
            openReceivables);

        return Ok(response);
    }

    // ---- Outstanding deposits report ----
    [HttpGet("reports/outstanding-deposits")]
    public async Task<ActionResult<OutstandingDepositsResponse>> GetOutstandingDepositsAsync(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var accounts = await _context.BankAccounts
            .Where(a => a.CompanyId == companyId && !a.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var dtos = new List<OutstandingDepositDto>();

        foreach (var account in accounts)
        {
            var outstanding = await _context.Deposits
                .Where(d => d.BankAccountId == account.Id && d.Status == DepositStatus.Draft && !d.DeletedOn.HasValue)
                .SumAsync(d => d.TotalAmount, cancellationToken);

            var count = await _context.Deposits
                .CountAsync(d => d.BankAccountId == account.Id && d.Status == DepositStatus.Draft && !d.DeletedOn.HasValue, cancellationToken);

            dtos.Add(new OutstandingDepositDto(account.Id, account.AccountName, outstanding, count));
        }

        return Ok(new OutstandingDepositsResponse(dtos, dtos.Sum(d => d.OutstandingDepositAmount)));
    }

    private static LockboxBatchDto MapLockbox(LockboxBatch b) => new(
        b.Id, b.CompanyId, b.BatchNumber, b.FileName, b.Format, b.ImportedOn, b.Status.ToString(), b.TotalItems, b.TotalAmount,
        b.Items.Select(i => new LockboxItemDto(i.Id, i.ReferenceNumber, i.CustomerId, i.CustomerName, i.Amount, i.RemittanceDate, i.InvoiceNumber, i.ReceiptCreated)).ToList());

    private static BankFeeAnalysisDto MapFeeAnalysis(BankFeeAnalysis a) => new(
        a.Id, a.CompanyId, a.Year, a.Month, a.GeneratedOn, a.TotalFees,
        a.Lines.Select(l => new BankFeeAnalysisLineDto(l.Id, l.FeeType, l.BankAccountId, l.Amount, l.Count)).ToList());
}
