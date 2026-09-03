// <copyright file="ReconciliationReportService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.EInvoice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Application;

/// <summary>
/// Service for generating reconciliation reports comparing ERP-posted documents
/// against MyInvois-confirmed e-Invoice documents.
/// </summary>
public interface IReconciliationReportService
{
    /// <summary>
    /// Generates a reconciliation report for a company within a date range.
    /// </summary>
    Task<ReconciliationReport> GetReconciliationReportAsync(
        Guid companyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents that are posted in ERP but not yet submitted to MyInvois.
    /// </summary>
    Task<List<ReconciliationItem>> GetUnsubmittedDocumentsAsync(
        Guid companyId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents that are submitted but not yet validated by MyInvois.
    /// </summary>
    Task<List<ReconciliationItem>> GetPendingValidationDocumentsAsync(
        Guid companyId,
        CancellationToken ct = default);
}

public class ReconciliationReportService : IReconciliationReportService
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<ReconciliationReportService> _logger;

    public ReconciliationReportService(EInvoiceDbContext dbContext, ILogger<ReconciliationReportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ReconciliationReport> GetReconciliationReportAsync(
        Guid companyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating reconciliation report for CompanyId={CompanyId}, From={From}, To={To}",
            companyId,
            fromDate,
            toDate);

        var documents = await _dbContext.EInvoiceDocuments
            .Where(d => d.CompanyId == companyId
                && d.CreatedAt >= fromDate
                && d.CreatedAt <= toDate)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

        var items = documents.Select(doc => new ReconciliationItem
        {
            DocumentId = doc.Id,
            SourceDocumentId = doc.SourceDocumentId,
            SourceDocumentType = doc.SourceDocumentType,
            DocumentType = doc.DocumentType,
            TotalAmount = doc.TotalAmount,
            TaxAmount = doc.TaxAmount,
            CurrencyCode = doc.CurrencyCode,
            Status = doc.Status.ToString(),
            MyInvoisUuid = doc.MyInvoisUuid,
            ValidationLink = doc.ValidationLink,
            ErrorMessage = doc.LhdnsErrorMessage,
            ErrorCode = doc.LhdnsErrorCode,
            SubmittedAt = doc.SubmittedAt,
            ValidatedAt = doc.ValidatedAt,
            CreatedAt = doc.CreatedAt,
        }).ToList();

        var validDocs = documents.Where(d => d.Status == EInvoiceDocumentStatus.Valid).ToList();
        var invalidDocs = documents.Where(d => d.Status == EInvoiceDocumentStatus.Invalid || d.Status == EInvoiceDocumentStatus.Rejected).ToList();
        var pendingDocs = documents.Where(d => d.Status == EInvoiceDocumentStatus.Submitted || d.Status == EInvoiceDocumentStatus.Validating).ToList();
        var cancelledDocs = documents.Where(d => d.Status == EInvoiceDocumentStatus.Cancelled).ToList();

        var totalAmount = documents.Sum(d => d.TotalAmount);
        var validAmount = validDocs.Sum(d => d.TotalAmount);
        var invalidAmount = invalidDocs.Sum(d => d.TotalAmount);

        var report = new ReconciliationReport
        {
            CompanyId = companyId,
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedAt = DateTime.UtcNow,
            Summary = new ReconciliationSummary
            {
                TotalDocuments = documents.Count,
                ValidDocuments = validDocs.Count,
                InvalidDocuments = invalidDocs.Count,
                PendingDocuments = pendingDocs.Count,
                CancelledDocuments = cancelledDocs.Count,
                TotalAmount = totalAmount,
                ValidAmount = validAmount,
                InvalidAmount = invalidAmount,
                SuccessRate = documents.Count > 0
                    ? Math.Round((decimal)validDocs.Count / documents.Count * 100, 1)
                    : 0,
            },
            Items = items,
            Discrepancies = BuildDiscrepancies(documents),
        };

        _logger.LogInformation(
            "Reconciliation report generated: {Total} documents, {Valid} valid, {Invalid} invalid, {Pending} pending",
            report.Summary.TotalDocuments,
            report.Summary.ValidDocuments,
            report.Summary.InvalidDocuments,
            report.Summary.PendingDocuments);

        return report;
    }

    public async Task<List<ReconciliationItem>> GetUnsubmittedDocumentsAsync(
        Guid companyId,
        CancellationToken ct = default)
    {
        // Documents that are queued but never submitted
        var docs = await _dbContext.EInvoiceDocuments
            .Where(d => d.CompanyId == companyId
                && d.Status == EInvoiceDocumentStatus.Queued)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(doc => new ReconciliationItem
        {
            DocumentId = doc.Id,
            SourceDocumentId = doc.SourceDocumentId,
            SourceDocumentType = doc.SourceDocumentType,
            DocumentType = doc.DocumentType,
            TotalAmount = doc.TotalAmount,
            TaxAmount = doc.TaxAmount,
            CurrencyCode = doc.CurrencyCode,
            Status = doc.Status.ToString(),
            CreatedAt = doc.CreatedAt,
        }).ToList();
    }

    public async Task<List<ReconciliationItem>> GetPendingValidationDocumentsAsync(
        Guid companyId,
        CancellationToken ct = default)
    {
        var docs = await _dbContext.EInvoiceDocuments
            .Where(d => d.CompanyId == companyId
                && (d.Status == EInvoiceDocumentStatus.Submitted || d.Status == EInvoiceDocumentStatus.Validating))
            .OrderBy(d => d.SubmittedAt)
            .ToListAsync(ct);

        return docs.Select(doc => new ReconciliationItem
        {
            DocumentId = doc.Id,
            SourceDocumentId = doc.SourceDocumentId,
            SourceDocumentType = doc.SourceDocumentType,
            DocumentType = doc.DocumentType,
            TotalAmount = doc.TotalAmount,
            TaxAmount = doc.TaxAmount,
            CurrencyCode = doc.CurrencyCode,
            Status = doc.Status.ToString(),
            MyInvoisUuid = doc.MyInvoisUuid,
            SubmittedAt = doc.SubmittedAt,
            CreatedAt = doc.CreatedAt,
        }).ToList();
    }

    private static List<ReconciliationDiscrepancy> BuildDiscrepancies(List<EInvoiceDocument> documents)
    {
        var discrepancies = new List<ReconciliationDiscrepancy>();

        // Find documents with errors
        var errorDocs = documents.Where(d =>
            d.Status == EInvoiceDocumentStatus.Invalid ||
            d.Status == EInvoiceDocumentStatus.Rejected).ToList();

        foreach (var doc in errorDocs)
        {
            discrepancies.Add(new ReconciliationDiscrepancy
            {
                Type = "SubmissionError",
                Severity = "High",
                DocumentId = doc.Id,
                Description = $"Document {doc.DocumentType} rejected by MyInvois",
                Detail = doc.LhdnsErrorMessage ?? "No error message",
                ErrorCode = doc.LhdnsErrorCode,
                Amount = doc.TotalAmount,
            });
        }

        // Find cancelled documents
        var cancelledDocs = documents.Where(d => d.Status == EInvoiceDocumentStatus.Cancelled).ToList();
        foreach (var doc in cancelledDocs)
        {
            discrepancies.Add(new ReconciliationDiscrepancy
            {
                Type = "Cancelled",
                Severity = "Medium",
                DocumentId = doc.Id,
                Description = $"Document {doc.DocumentType} was cancelled",
                Detail = "Document was cancelled within the 72-hour window",
                Amount = doc.TotalAmount,
            });
        }

        // Find documents pending too long (> 24 hours since submission)
        var stalePending = documents.Where(d =>
            (d.Status == EInvoiceDocumentStatus.Submitted || d.Status == EInvoiceDocumentStatus.Validating)
            && d.SubmittedAt.HasValue
            && (DateTime.UtcNow - d.SubmittedAt.Value).TotalHours > 24).ToList();

        foreach (var doc in stalePending)
        {
            var hoursPending = (DateTime.UtcNow - doc.SubmittedAt!.Value).TotalHours;
            discrepancies.Add(new ReconciliationDiscrepancy
            {
                Type = "StalePending",
                Severity = "Medium",
                DocumentId = doc.Id,
                Description = $"Document pending for {hoursPending:F0} hours",
                Detail = "Document has been in pending state for more than 24 hours",
                Amount = doc.TotalAmount,
            });
        }

        return discrepancies;
    }
}

/// <summary>
/// Reconciliation report comparing ERP vs MyInvois.
/// </summary>
public class ReconciliationReport
{
    /// <summary>Gets or sets the company ID.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the report start date.</summary>
    public DateTime FromDate { get; set; }

    /// <summary>Gets or sets the report end date.</summary>
    public DateTime ToDate { get; set; }

    /// <summary>Gets or sets when the report was generated.</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>Gets or sets the summary statistics.</summary>
    public ReconciliationSummary Summary { get; set; } = new();

    /// <summary>Gets or sets the individual document items.</summary>
    public List<ReconciliationItem> Items { get; set; } = new();

    /// <summary>Gets or sets the discrepancies found.</summary>
    public List<ReconciliationDiscrepancy> Discrepancies { get; set; } = new();
}

/// <summary>
/// Summary statistics for reconciliation.
/// </summary>
public class ReconciliationSummary
{
    public int TotalDocuments { get; set; }
    public int ValidDocuments { get; set; }
    public int InvalidDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int CancelledDocuments { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ValidAmount { get; set; }
    public decimal InvalidAmount { get; set; }
    public decimal SuccessRate { get; set; }
}

/// <summary>
/// Individual reconciliation item.
/// </summary>
public class ReconciliationItem
{
    public Guid DocumentId { get; set; }
    public Guid SourceDocumentId { get; set; }
    public string SourceDocumentType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MyInvoisUuid { get; set; }
    public string? ValidationLink { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Reconciliation discrepancy.
/// </summary>
public class ReconciliationDiscrepancy
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public decimal Amount { get; set; }
}
