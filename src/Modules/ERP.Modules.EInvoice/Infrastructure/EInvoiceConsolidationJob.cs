// <copyright file="EInvoiceConsolidationJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Consolidates multiple small-value e-Invoice documents into a single submission.
/// Per LHDN MyInvois rules, businesses may consolidate individual invoices under RM10,000
/// into a single consolidated e-Invoice for simplified reporting.
/// This job runs daily and picks up unconsolidated documents.
/// </summary>
public class EInvoiceConsolidationJob
{
    /// <summary>Maximum amount per line item for consolidation eligibility (RM10,000).</summary>
    private const decimal MaxConsolidationAmount = 10_000m;

    /// <summary>Maximum number of line items per consolidated document (LHDN limit).</summary>
    private const int MaxLineItems = 100;

    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<EInvoiceConsolidationJob> _logger;

    public EInvoiceConsolidationJob(
        EInvoiceDbContext dbContext,
        ILogger<EInvoiceConsolidationJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Groups eligible invoices by company and creates consolidated e-Invoice documents.
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
                await CreateConsolidatedDocumentAsync(companyId, batch, ct);
            }
        }

        _logger.LogInformation("Consolidated e-Invoice batch job completed.");
    }

    private async Task CreateConsolidatedDocumentAsync(
        Guid companyId,
        List<EInvoiceDocument> documents,
        CancellationToken ct)
    {
        var totalAmount = documents.Sum(d => d.TotalAmount);
        var totalTax = documents.Sum(d => d.TaxAmount);

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
    }
}
