// <copyright file="ArPhase4Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
using Asp.Versioning;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsReceivable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ar")]
public class ArPhase4Controller : ControllerBase
{
    private readonly ArDbContext _context;

    public ArPhase4Controller(ArDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ---------------------------------------------------------------------
    // Collection Notes (Collections workflow)
    // ---------------------------------------------------------------------
    [HttpGet("collection-notes")]
    public async Task<ActionResult<IReadOnlyList<CollectionNoteDto>>> GetCollectionNotesAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? assignedTo,
        [FromQuery] CollectionNoteStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.CollectionNotes.Where(n => n.CompanyId == companyId && !n.DeletedOn.HasValue);

        if (customerId.HasValue)
            query = query.Where(n => n.CustomerId == customerId.Value);
        if (assignedTo.HasValue)
            query = query.Where(n => n.AssignedTo == assignedTo.Value);
        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        var notes = await query
            .OrderByDescending(n => n.CreatedOn)
            .Select(n => new CollectionNoteDto(
                n.Id,
                n.CompanyId,
                n.CustomerId,
                n.Note,
                n.Author,
                n.Type,
                n.Status,
                n.AssignedTo,
                n.FollowUpDate,
                n.PromiseToPayDate,
                n.RelatedDocumentNumber))
            .ToListAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpPost("collection-notes")]
    public async Task<ActionResult<CollectionNoteDto>> CreateCollectionNoteAsync(
        CreateCollectionNoteRequest request,
        CancellationToken cancellationToken)
    {
        var note = new CollectionNote(
            request.CompanyId,
            request.CustomerId,
            request.Note,
            request.Author,
            request.Type,
            request.AssignedTo,
            request.FollowUpDate,
            request.RelatedDocumentNumber);

        if (request.PromiseToPayDate.HasValue)
            note.SetPromiseToPay(request.PromiseToPayDate.Value);

        _context.CollectionNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapNote(note));
    }

    [HttpPost("collection-notes/{id:guid}/activity")]
    public async Task<ActionResult<CollectionNoteDto>> AddCollectionNoteActivityAsync(
        Guid id,
        AddCollectionNoteActivityRequest request,
        CancellationToken cancellationToken)
    {
        var note = await _context.CollectionNotes.FirstOrDefaultAsync(n => n.Id == id && !n.DeletedOn.HasValue, cancellationToken);
        if (note == null)
            return NotFound();

        note.AddActivity(request.Author, request.Description, request.ActivityType);

        if (request.PromiseToPayDate.HasValue)
            note.SetPromiseToPay(request.PromiseToPayDate.Value);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapNote(note));
    }

    [HttpPost("collection-notes/{id:guid}/assign")]
    public async Task<ActionResult<CollectionNoteDto>> AssignCollectionNoteAsync(
        Guid id,
        AssignCollectionNoteRequest request,
        CancellationToken cancellationToken)
    {
        var note = await _context.CollectionNotes.FirstOrDefaultAsync(n => n.Id == id && !n.DeletedOn.HasValue, cancellationToken);
        if (note == null)
            return NotFound();

        note.Assign(request.AssignedTo);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapNote(note));
    }

    [HttpPost("collection-notes/{id:guid}/close")]
    public async Task<ActionResult<CollectionNoteDto>> CloseCollectionNoteAsync(
        Guid id,
        CloseCollectionNoteRequest request,
        CancellationToken cancellationToken)
    {
        var note = await _context.CollectionNotes.FirstOrDefaultAsync(n => n.Id == id && !n.DeletedOn.HasValue, cancellationToken);
        if (note == null)
            return NotFound();

        note.Close(request.Author);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapNote(note));
    }

    [HttpPost("collection-notes/{id:guid}/reopen")]
    public async Task<ActionResult<CollectionNoteDto>> ReopenCollectionNoteAsync(
        Guid id,
        CloseCollectionNoteRequest request,
        CancellationToken cancellationToken)
    {
        var note = await _context.CollectionNotes.FirstOrDefaultAsync(n => n.Id == id && !n.DeletedOn.HasValue, cancellationToken);
        if (note == null)
            return NotFound();

        note.Reopen(request.Author);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapNote(note));
    }

    // ---------------------------------------------------------------------
    // Collections Dashboard (per collector queue)
    // ---------------------------------------------------------------------
    [HttpGet("collections-dashboard")]
    public async Task<ActionResult<CollectionsDashboardDto>> GetCollectionsDashboardAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? assignedTo,
        CancellationToken cancellationToken)
    {
        var notesQuery = _context.CollectionNotes.Where(n => n.CompanyId == companyId && !n.DeletedOn.HasValue && n.Status == CollectionNoteStatus.Open);
        if (assignedTo.HasValue)
            notesQuery = notesQuery.Where(n => n.AssignedTo == assignedTo.Value);

        var notes = await notesQuery.ToListAsync(cancellationToken);

        var followUpQueue = notes
            .Where(n => n.FollowUpDate.HasValue)
            .OrderBy(n => n.FollowUpDate)
            .Select(n => new CollectionsQueueItemDto(
                n.Id,
                n.CustomerId,
                n.FollowUpDate!.Value,
                n.PromiseToPayDate,
                n.Type,
                n.AssignedTo))
            .ToList();

        var agingTotals = await ComputeAgingTotalsAsync(cancellationToken);

        return Ok(new CollectionsDashboardDto(
            companyId,
            notes.Count,
            notes.Count(n => n.PromiseToPayDate.HasValue),
            followUpQueue,
            agingTotals));
    }

    // ---------------------------------------------------------------------
    // Dunning Templates
    // ---------------------------------------------------------------------
    [HttpGet("dunning-templates")]
    public async Task<ActionResult<IReadOnlyList<DunningTemplateDto>>> GetDunningTemplatesAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var templates = await _context.DunningTemplates
            .Where(t => t.CompanyId == companyId && !t.DeletedOn.HasValue)
            .OrderBy(t => t.Sequence)
            .Select(t => new DunningTemplateDto(
                t.Id,
                t.CompanyId,
                t.Name,
                t.Subject,
                t.Body,
                t.Sequence,
                t.Bucket,
                t.MinDaysOverdue,
                t.MaxDaysOverdue,
                t.SendEmail,
                t.SendPdf,
                t.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(templates);
    }

    [HttpPost("dunning-templates")]
    public async Task<ActionResult<DunningTemplateDto>> CreateDunningTemplateAsync(
        CreateDunningTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = new DunningTemplate(
            request.CompanyId,
            request.Name,
            request.Subject,
            request.Body,
            request.Sequence,
            request.Bucket,
            request.MinDaysOverdue,
            request.MaxDaysOverdue,
            request.SendEmail,
            request.SendPdf);

        _context.DunningTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapDunning(template));
    }

    [HttpPut("dunning-templates/{id:guid}")]
    public async Task<ActionResult<DunningTemplateDto>> UpdateDunningTemplateAsync(
        Guid id,
        UpdateDunningTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _context.DunningTemplates.FirstOrDefaultAsync(t => t.Id == id && !t.DeletedOn.HasValue, cancellationToken);
        if (template == null)
            return NotFound();

        template.Update(
            request.Name,
            request.Subject,
            request.Body,
            request.Sequence,
            request.Bucket,
            request.MinDaysOverdue,
            request.MaxDaysOverdue,
            request.SendEmail,
            request.SendPdf,
            request.IsActive);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapDunning(template));
    }

    [HttpPost("dunning-templates/run")]
    public async Task<ActionResult<DunningRunResultDto>> RunDunningAsync(
        RunDunningRequest request,
        CancellationToken cancellationToken)
    {
        var asOfDate = request.AsOfDate ?? DateTimeOffset.UtcNow;
        var templates = await _context.DunningTemplates
            .Where(t => t.CompanyId == request.CompanyId && t.IsActive && !t.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        if (templates.Count == 0)
            return Ok(new DunningRunResultDto(request.CompanyId, asOfDate, 0, []));

        var customers = await _context.Customers.Where(c => !c.DeletedOn.HasValue).ToListAsync(cancellationToken);
        var generated = new List<DunningLetterDto>();

        foreach (var customer in customers)
        {
            var openInvoices = await _context.Invoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customer.Id && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Paid)
                .ToListAsync(cancellationToken);

            foreach (var invoice in openInvoices)
            {
                var daysOverdue = (asOfDate - invoice.DueDate).Days;
                foreach (var template in templates)
                {
                    if (daysOverdue >= template.MinDaysOverdue && daysOverdue <= template.MaxDaysOverdue)
                    {
                        generated.Add(new DunningLetterDto(
                            invoice.Id,
                            customer.Id,
                            customer.Name,
                            invoice.InvoiceNumber,
                            template.Sequence,
                            template.Bucket,
                            template.Subject,
                            template.Body,
                            invoice.BalanceDue,
                            daysOverdue));
                    }
                }
            }
        }

        return Ok(new DunningRunResultDto(request.CompanyId, asOfDate, generated.Count, generated));
    }

    // ---------------------------------------------------------------------
    // Allowance for Doubtful Accounts (bad-debt reserve)
    // ---------------------------------------------------------------------
    [HttpPost("allowance-runs")]
    public async Task<ActionResult<AllowanceRunDto>> CreateAllowanceRunAsync(
        CreateAllowanceRunRequest request,
        CancellationToken cancellationToken)
    {
        var run = new DoubtfulAccountAllowance(
            request.CompanyId,
            request.AsOfDate,
            Guid.Empty,
            request.Name);
        run.Name = request.Name;
        run.Method = request.Method;

        var aging = await ComputeAgingBreakdownAsync(request.AsOfDate, cancellationToken);

        var rates = request.Method switch
        {
            AllowanceMethod.PercentageOfReceivables => aging.ToDictionary(b => b.Bucket, b => request.PercentageOfReceivables / 100m),
            AllowanceMethod.AgingCategories => new Dictionary<DunningAgingBucket, decimal>
            {
                [DunningAgingBucket.Current] = request.AgingRateCurrent / 100m,
                [DunningAgingBucket.Days1To30] = request.AgingRate1To30 / 100m,
                [DunningAgingBucket.Days31To60] = request.AgingRate31To60 / 100m,
                [DunningAgingBucket.Days61To90] = request.AgingRate61To90 / 100m,
                [DunningAgingBucket.Over90] = request.AgingRateOver90 / 100m,
            },
            AllowanceMethod.Specific => aging.ToDictionary(b => b.Bucket, b => request.SpecificAmount > 0 ? 1m : 0m),
            _ => aging.ToDictionary(b => b.Bucket, b => 0m),
        };

        foreach (var bucket in aging)
        {
            if (rates.TryGetValue(bucket.Bucket, out var rate))
            {
                var estimated = Math.Round(bucket.Outstanding * rate, 2);
                run.AddBucket(bucket.Bucket, bucket.Outstanding, rate, estimated);
            }
        }

        _context.DoubtfulAccountAllowances.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapAllowance(run));
    }

    [HttpGet("allowance-runs")]
    public async Task<ActionResult<IReadOnlyList<AllowanceRunDto>>> GetAllowanceRunsAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var runs = await _context.DoubtfulAccountAllowances
            .Where(r => r.CompanyId == companyId && !r.DeletedOn.HasValue)
            .OrderByDescending(r => r.AsOfDate)
            .Select(r => new AllowanceRunDto(
                r.Id,
                r.CompanyId,
                r.AsOfDate,
                r.ReserveAccountId,
                r.Name,
                r.Method,
                r.Notes,
                r.Status,
                r.TotalEstimatedAllowance,
                r.PostedBy,
                r.PostedOn,
                r.Buckets.Select(b => new AllowanceBucketDto(
                    b.Bucket,
                    b.OutstandingBalance,
                    b.ReserveRate,
                    b.EstimatedAmount)).ToList()))
            .ToListAsync(cancellationToken);

        return Ok(runs);
    }

    [HttpPost("allowance-runs/{id:guid}/post")]
    public async Task<ActionResult<AllowanceRunDto>> PostAllowanceRunAsync(
        Guid id,
        PostAllowanceRunRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _context.DoubtfulAccountAllowances
            .Include(r => r.Buckets)
            .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedOn.HasValue, cancellationToken);
        if (run == null)
            return NotFound();

        run.Post(request.PostedBy);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapAllowance(run));
    }

    // ---------------------------------------------------------------------
    // Resale Certificates (tax-exempt certificate management)
    // ---------------------------------------------------------------------
    [HttpGet("resale-certificates")]
    public async Task<ActionResult<IReadOnlyList<ResaleCertificateDto>>> GetResaleCertificatesAsync(
        [FromQuery] Guid companyId,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var query = _context.ResaleCertificates.Where(c => c.CompanyId == companyId && !c.DeletedOn.HasValue);
        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        var certs = await query
            .OrderByDescending(c => c.ExpiryDate)
            .Select(c => new ResaleCertificateDto(
                c.Id,
                c.CompanyId,
                c.CustomerId,
                c.CertificateNumber,
                c.IssuedState,
                c.IssueDate,
                c.ExpiryDate,
                c.DocumentReference,
                c.IsActive,
                c.IsExpired))
            .ToListAsync(cancellationToken);

        return Ok(certs);
    }

    [HttpPost("resale-certificates")]
    public async Task<ActionResult<ResaleCertificateDto>> CreateResaleCertificateAsync(
        CreateResaleCertificateRequest request,
        CancellationToken cancellationToken)
    {
        var cert = new ResaleCertificate(
            request.CompanyId,
            request.CustomerId,
            request.CertificateNumber,
            request.IssuedState,
            request.IssueDate,
            request.ExpiryDate,
            request.DocumentReference);

        _context.ResaleCertificates.Add(cert);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapResale(cert));
    }

    [HttpPut("resale-certificates/{id:guid}")]
    public async Task<ActionResult<ResaleCertificateDto>> UpdateResaleCertificateAsync(
        Guid id,
        UpdateResaleCertificateRequest request,
        CancellationToken cancellationToken)
    {
        var cert = await _context.ResaleCertificates.FirstOrDefaultAsync(c => c.Id == id && !c.DeletedOn.HasValue, cancellationToken);
        if (cert == null)
            return NotFound();

        cert.Update(
            request.CertificateNumber,
            request.IssuedState,
            request.IssueDate,
            request.ExpiryDate,
            request.DocumentReference,
            request.IsActive);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(MapResale(cert));
    }

    // ---------------------------------------------------------------------
    // Credit Memo Application (distinct from cash application)
    // ---------------------------------------------------------------------
    [HttpPost("credit-memos/{id:guid}/apply")]
    public async Task<ActionResult<CreditMemoApplyResultDto>> ApplyCreditMemoAsync(
        Guid id,
        ApplyCreditMemoRequest request,
        CancellationToken cancellationToken)
    {
        var memo = await _context.CreditDebitMemos
            .Include(m => m.Lines)
            .FirstOrDefaultAsync(m => m.Id == id && m.MemoType == CreditDebitMemoType.CreditMemo, cancellationToken);
        if (memo == null)
            return NotFound();
        if (memo.Status != CreditDebitMemoStatus.Open)
            return BadRequest("Credit memo is not in Open status.");

        var remaining = memo.TotalAmount;
        List<Guid> appliedInvoiceIds = [];

        var candidateInvoices = await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == memo.CustomerId && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Paid)
            .ToListAsync(cancellationToken);

        var targetInvoices = request.TargetInvoiceIds != null && request.TargetInvoiceIds.Count > 0
            ? candidateInvoices
                .Where(i => request.TargetInvoiceIds.Contains(i.Id) && i.BalanceDue > 0)
                .OrderBy(i => i.DueDate)
                .ToList()
            : candidateInvoices
                .Where(i => i.BalanceDue > 0)
                .OrderBy(i => i.BalanceDue)
                .ToList();

        foreach (var invoice in targetInvoices)
        {
            if (remaining <= 0)
                break;

            var applyAmount = Math.Min(remaining, invoice.BalanceDue);
            invoice.ApplyPayment(applyAmount);
            remaining -= applyAmount;
            appliedInvoiceIds.Add(invoice.Id);
        }

        if (appliedInvoiceIds.Count == 0)
            return BadRequest("No eligible invoices to apply the credit memo against.");

        memo.Apply();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new CreditMemoApplyResultDto(memo.Id, memo.TotalAmount, appliedInvoiceIds));
    }

    // ---------------------------------------------------------------------
    // Cash Receipt matching by reference
    // ---------------------------------------------------------------------
    [HttpPost("cash-receipts/{id:guid}/match-by-reference")]
    public async Task<ActionResult<CashReceiptReferenceMatchDto>> MatchCashReceiptByReferenceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var receipt = await _context.CashReceipts
            .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedOn.HasValue, cancellationToken);
        if (receipt == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(receipt.ReferenceNumber))
            return BadRequest("Receipt has no reference number to match.");

        var candidateInvoices = await _context.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CustomerId == receipt.CustomerId
                && i.Status != InvoiceStatus.Voided
                && i.Status != InvoiceStatus.Paid)
            .ToListAsync(cancellationToken);

        var matchedInvoices = candidateInvoices
            .Where(i => (i.InvoiceNumber == receipt.ReferenceNumber || (i.Description != null && i.Description.Contains(receipt.ReferenceNumber, StringComparison.Ordinal)))
                && i.BalanceDue > 0)
            .OrderByDescending(i => i.InvoiceDate)
            .ToList();

        var applied = 0m;
        var appliedInvoiceIds = new List<Guid>();
        var remaining = receipt.UnappliedAmount;

        foreach (var invoice in matchedInvoices)
        {
            if (remaining <= 0)
                break;

            var applyAmount = Math.Min(remaining, invoice.BalanceDue);
            receipt.ApplyToInvoice(invoice, applyAmount);
            applied += applyAmount;
            remaining -= applyAmount;
            appliedInvoiceIds.Add(invoice.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new CashReceiptReferenceMatchDto(
            receipt.Id,
            receipt.ReferenceNumber,
            matchedInvoices.Count,
            applied,
            appliedInvoiceIds));
    }

    // ---------------------------------------------------------------------
    // Aging by due date vs invoice date
    // ---------------------------------------------------------------------
    [HttpGet("reports/aging-by-basis")]
    public async Task<ActionResult<ArAgingByBasisReportDto>> GetAgingByBasisAsync(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken,
        [FromQuery] string basis = "DueDate")
    {
        var asOfDate = DateTimeOffset.UtcNow;
        var useInvoiceDate = basis.Equals("InvoiceDate", StringComparison.OrdinalIgnoreCase);

        var customers = await _context.Customers
            .Where(c => !c.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var breakdown = new List<AgingBucketBreakdownDto>
        {
            new(DunningAgingBucket.Current),
            new(DunningAgingBucket.Days1To30),
            new(DunningAgingBucket.Days31To60),
            new(DunningAgingBucket.Days61To90),
            new(DunningAgingBucket.Over90),
        };

        foreach (var customer in customers)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customer.Id
                    && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Paid)
                .ToListAsync(cancellationToken);

            foreach (var inv in invoices)
            {
                var basisDate = useInvoiceDate ? inv.InvoiceDate : inv.DueDate;
                var days = (asOfDate - basisDate).Days;
                var balance = inv.BalanceDue;

                DunningAgingBucket bucket;
                if (days <= 0)
                    bucket = DunningAgingBucket.Current;
                else if (days <= 30)
                    bucket = DunningAgingBucket.Days1To30;
                else if (days <= 60)
                    bucket = DunningAgingBucket.Days31To60;
                else if (days <= 90)
                    bucket = DunningAgingBucket.Days61To90;
                else
                    bucket = DunningAgingBucket.Over90;

                breakdown.Single(b => b.Bucket == bucket).Outstanding += balance;
            }
        }

        return Ok(new ArAgingByBasisReportDto(
            companyId,
            basis,
            asOfDate,
            breakdown,
            breakdown.Sum(b => b.Outstanding),
            DateTimeOffset.UtcNow));
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private static CollectionNoteDto MapNote(CollectionNote n) => new(
        n.Id, n.CompanyId, n.CustomerId, n.Note, n.Author, n.Type, n.Status,
        n.AssignedTo, n.FollowUpDate, n.PromiseToPayDate, n.RelatedDocumentNumber);

    private static DunningTemplateDto MapDunning(DunningTemplate t) => new(
        t.Id, t.CompanyId, t.Name, t.Subject, t.Body, t.Sequence, t.Bucket,
        t.MinDaysOverdue, t.MaxDaysOverdue, t.SendEmail, t.SendPdf, t.IsActive);

    private static ResaleCertificateDto MapResale(ResaleCertificate c) => new(
        c.Id, c.CompanyId, c.CustomerId, c.CertificateNumber, c.IssuedState,
        c.IssueDate, c.ExpiryDate, c.DocumentReference, c.IsActive, c.IsExpired);

    private static AllowanceRunDto MapAllowance(DoubtfulAccountAllowance r) => new(
        r.Id, r.CompanyId, r.AsOfDate, r.ReserveAccountId, r.Name, r.Method, r.Notes, r.Status,
        r.TotalEstimatedAllowance, r.PostedBy, r.PostedOn,
        r.Buckets.Select(b => new AllowanceBucketDto(b.Bucket, b.OutstandingBalance, b.ReserveRate, b.EstimatedAmount)).ToList());

    private async Task<List<AgingBucketBreakdownDto>> ComputeAgingBreakdownAsync(
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken)
    {
        var customers = await _context.Customers.Where(c => !c.DeletedOn.HasValue).ToListAsync(cancellationToken);
        var result = new List<AgingBucketBreakdownDto>();

        foreach (DunningAgingBucket bucket in Enum.GetValues<DunningAgingBucket>())
        {
            result.Add(new AgingBucketBreakdownDto(bucket) { Outstanding = 0m });
        }

        foreach (var customer in customers)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customer.Id && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Paid)
                .ToListAsync(cancellationToken);

            foreach (var inv in invoices)
            {
                var days = (asOfDate - inv.DueDate).Days;
                DunningAgingBucket bucket;
                if (days <= 0)
                    bucket = DunningAgingBucket.Current;
                else if (days <= 30)
                    bucket = DunningAgingBucket.Days1To30;
                else if (days <= 60)
                    bucket = DunningAgingBucket.Days31To60;
                else if (days <= 90)
                    bucket = DunningAgingBucket.Days61To90;
                else
                    bucket = DunningAgingBucket.Over90;
                result.First(b => b.Bucket == bucket).Outstanding += inv.BalanceDue;
            }
        }

        return result;
    }

    private async Task<decimal> ComputeAgingTotalsAsync(CancellationToken cancellationToken)
    {
        var customers = await _context.Customers.Where(c => !c.DeletedOn.HasValue).ToListAsync(cancellationToken);
        var total = 0m;
        foreach (var customer in customers)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Lines)
                .Where(i => i.CustomerId == customer.Id && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Paid)
                .ToListAsync(cancellationToken);
            total += invoices.Sum(i => i.BalanceDue);
        }

        return total;
    }
}
