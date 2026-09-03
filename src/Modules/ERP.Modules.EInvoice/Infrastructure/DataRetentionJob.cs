// <copyright file="DataRetentionJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Hangfire background job that archives e-Invoice documents older than 7 years.
/// Malaysian tax law requires retention of e-Invoice records for a minimum of 7 years.
/// This job runs monthly and moves old documents to an archive table.
/// </summary>
public class DataRetentionJob
{
    /// <summary>Minimum retention period in years per Malaysian tax law.</summary>
    private const int RetentionYears = 7;

    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(
        EInvoiceDbContext dbContext,
        ILogger<DataRetentionJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Archives documents older than the retention period.
    /// Documents must be in a terminal state (Valid, Invalid, Cancelled, Rejected) before archival.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-RetentionYears);

        _logger.LogInformation(
            "Data retention job started. Archiving documents created before {CutoffDate:yyyy-MM-dd} (7+ years old)",
            cutoffDate);

        var documentsToArchive = await _dbContext.EInvoiceDocuments
            .Where(d => d.CreatedAt < cutoffDate
                && (d.Status == EInvoiceDocumentStatus.Valid
                    || d.Status == EInvoiceDocumentStatus.Invalid
                    || d.Status == EInvoiceDocumentStatus.Cancelled
                    || d.Status == EInvoiceDocumentStatus.Rejected))
            .OrderBy(d => d.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        if (documentsToArchive.Count == 0)
        {
            _logger.LogInformation(
                "No documents older than {Years} years to archive.",
                RetentionYears);
            return;
        }

        _logger.LogInformation(
            "Archiving {Count} documents created before {CutoffDate:yyyy-MM-dd}",
            documentsToArchive.Count,
            cutoffDate);

        foreach (var doc in documentsToArchive)
        {
            var archive = new EInvoiceDocumentArchive
            {
                Id = doc.Id,
                CompanyId = doc.CompanyId,
                SourceDocumentId = doc.SourceDocumentId,
                SourceDocumentType = doc.SourceDocumentType,
                DocumentType = doc.DocumentType,
                TotalAmount = doc.TotalAmount,
                TaxAmount = doc.TaxAmount,
                TaxCodeId = doc.TaxCodeId,
                CurrencyCode = doc.CurrencyCode,
                SupplierTIN = doc.SupplierTIN,
                CustomerTIN = doc.CustomerTIN,
                MyInvoisUuid = doc.MyInvoisUuid,
                ValidationLink = doc.ValidationLink,
                Status = doc.Status.ToString(),
                LhdnsErrorCode = doc.LhdnsErrorCode,
                LhdnsErrorMessage = doc.LhdnsErrorMessage,
                IsSandbox = doc.IsSandbox,
                CreatedAt = doc.CreatedAt,
                SubmittedAt = doc.SubmittedAt,
                ValidatedAt = doc.ValidatedAt,
                CancelledAt = doc.CancelledAt,
                SubmissionUid = doc.SubmissionUid,
                ArchivedAt = DateTime.UtcNow,
            };

            _dbContext.EInvoiceDocumentArchives.Add(archive);
        }

        _dbContext.EInvoiceDocuments.RemoveRange(documentsToArchive);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Successfully archived {Count} documents. Cutoff: {CutoffDate:yyyy-MM-dd}",
            documentsToArchive.Count,
            cutoffDate);
    }
}
