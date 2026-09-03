// <copyright file="EInvoiceConsolidationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Consolidates multiple small-value e-Invoice documents into a single submission.
/// Per LHDN MyInvois rules, businesses may consolidate individual invoices under RM10,000
/// into a single consolidated e-Invoice for simplified reporting.
/// This job runs daily, creates consolidated documents, and submits them via the standard pipeline.
/// </summary>
public class EInvoiceConsolidationJob
{
    /// <summary>Maximum amount per line item for consolidation eligibility (RM10,000).</summary>
    private const decimal MaxConsolidationAmount = 10_000m;

    /// <summary>Maximum number of line items per consolidated document (LHDN limit).</summary>
    private const int MaxLineItems = 100;

    private readonly EInvoiceDbContext _dbContext;
    private readonly IEInvoiceService _eInvoiceService;
    private readonly ILogger<EInvoiceConsolidationJob> _logger;

    public EInvoiceConsolidationJob(
        EInvoiceDbContext dbContext,
        IEInvoiceService eInvoiceService,
        ILogger<EInvoiceConsolidationJob> logger)
    {
        _dbContext = dbContext;
        _eInvoiceService = eInvoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Groups eligible invoices by company, creates consolidated e-Invoice documents,
    /// and submits them to MyInvois via the standard submission pipeline.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Consolidated e-Invoice batch job started.");

        var eligibleDocs = await _dbContext.EInvoiceDocuments
            .Where(d => d.Status == EInvoiceDocumentStatus.Valid
                && d.TotalAmount <= MaxConsolidationAmount
                && d.DocumentType == "Invoice"
                && d.ConsolidatedDocumentId == null)
            .OrderBy(d => d.CompanyId)
            .ThenBy(d => d.CreatedAt)
            .Take(MaxLineItems * 10)
            .ToListAsync(ct);

        if (eligibleDocs.Count == 0)
        {
            _logger.LogInformation("No documents eligible for consolidation.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} documents eligible for consolidation.",
            eligibleDocs.Count);

        var grouped = eligibleDocs
            .GroupBy(d => d.CompanyId)
            .ToList();

        var submittedCount = 0;
        var failedCount = 0;

        foreach (var companyGroup in grouped)
        {
            var companyId = companyGroup.Key;
            var docs = companyGroup.ToList();

            _logger.LogInformation(
                "Consolidating {Count} documents for company {CompanyId}",
                docs.Count,
                companyId);

            for (var i = 0; i < docs.Count; i += MaxLineItems)
            {
                var batch = docs.Skip(i).Take(MaxLineItems).ToList();
                var result = await CreateAndSubmitConsolidatedDocumentAsync(companyId, batch, ct);

                if (result)
                {
                    submittedCount++;
                }
                else
                {
                    failedCount++;
                }
            }
        }

        _logger.LogInformation(
            "Consolidated e-Invoice batch job completed. Submitted: {Submitted}, Failed: {Failed}",
            submittedCount,
            failedCount);
    }

    private async Task<bool> CreateAndSubmitConsolidatedDocumentAsync(
        Guid companyId,
        List<EInvoiceDocument> documents,
        CancellationToken ct)
    {
        var totalAmount = documents.Sum(d => d.TotalAmount);
        var totalTax = documents.Sum(d => d.TaxAmount);
        var consolidatedNumber = $"CONS-{companyId.ToString()[..8]}-{DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)}";

        // Create the consolidated document record
        var consolidatedDoc = new EInvoiceDocument
        {
            CompanyId = companyId,
            SourceDocumentId = documents[0].SourceDocumentId,
            SourceDocumentType = "ConsolidatedInvoice",
            DocumentType = "ConsolidatedInvoice",
            TotalAmount = totalAmount,
            TaxAmount = totalTax,
            CurrencyCode = "MYR",
            Status = EInvoiceDocumentStatus.Queued,
            ContentType = "JSON",
            IsSandbox = documents[0].IsSandbox,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.EInvoiceDocuments.Add(consolidatedDoc);

        foreach (var doc in documents)
        {
            doc.ConsolidatedDocumentId = consolidatedDoc.Id;
            doc.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created consolidated document {ConsolidatedId} with {LineCount} lines, total {TotalAmount:N2}",
            consolidatedDoc.Id,
            documents.Count,
            totalAmount);

        // Build consolidated UBL payload for submission
        var lineItems = new List<UblInvoiceLineDto>();
        var lineNum = 1;
        foreach (var doc in documents)
        {
            var lineAmount = doc.TotalAmount - doc.TaxAmount;
            lineItems.Add(new UblInvoiceLineDto
            {
                LineNumber = lineNum++,
                Description = $"Consolidated line from doc {doc.Id}",
                Quantity = 1,
                UnitCode = "C62",
                UnitPrice = lineAmount,
                LineExtensionAmount = lineAmount,
                TaxAmount = doc.TaxAmount,
                TaxTypeCode = "01",
                TaxRatePercent = lineAmount > 0
                    ? Math.Round(doc.TaxAmount / lineAmount * 100m, 2)
                    : 0m,
            });
        }

        var payload = JsonSerializer.Serialize(new
        {
            InvoiceNumber = consolidatedNumber,
            InvoiceTypeCode = "01",
            IssueDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            CurrencyCode = "MYR",
            TotalAmount = totalAmount,
            TaxAmount = totalTax,
            LineItems = lineItems,
            SourceDocumentIds = documents.Select(d => d.Id).ToList(),
        });

        // Submit via standard pipeline
        try
        {
            await _eInvoiceService.SubmitDocumentAsync(
                companyId,
                consolidatedDoc.SourceDocumentId,
                consolidatedDoc.DocumentType,
                payload,
                cancellationToken: ct);

            _logger.LogInformation(
                "Submitted consolidated document {Id} to MyInvois",
                consolidatedDoc.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to submit consolidated document {Id}",
                consolidatedDoc.Id);
            return false;
        }
    }
}
