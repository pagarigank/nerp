// <copyright file="EInvoiceDocumentArchive.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// Archive copy of an e-Invoice document for long-term retention.
/// Malaysian tax law requires 7-year retention. Documents are moved here
/// from the active EInvoiceDocument table by the DataRetentionJob.
/// </summary>
[Table("einvoice_document_archive", Schema = "int")]
public class EInvoiceDocumentArchive
{
    /// <summary>Gets or sets the original document ID (preserved for reference).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the company scoping identifier.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the ERP source document ID.</summary>
    public Guid SourceDocumentId { get; set; }

    /// <summary>Gets or sets the source document type.</summary>
    [MaxLength(50)]
    public string SourceDocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the document type (Invoice, CreditNote, etc.).</summary>
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the total amount.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the tax amount.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the tax code ID.</summary>
    [MaxLength(50)]
    public string? TaxCodeId { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "MYR";

    /// <summary>Gets or sets the supplier TIN.</summary>
    [MaxLength(50)]
    public string? SupplierTIN { get; set; }

    /// <summary>Gets or sets the customer TIN.</summary>
    [MaxLength(50)]
    public string? CustomerTIN { get; set; }

    /// <summary>Gets or sets the MyInvois UUID.</summary>
    [MaxLength(100)]
    public string? MyInvoisUuid { get; set; }

    /// <summary>Gets or sets the validation link.</summary>
    [MaxLength(500)]
    public string? ValidationLink { get; set; }

    /// <summary>Gets or sets the final status (stored as string for archival).</summary>
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the LHDN error code.</summary>
    [MaxLength(50)]
    public string? LhdnsErrorCode { get; set; }

    /// <summary>Gets or sets the LHDN error message.</summary>
    [MaxLength(500)]
    public string? LhdnsErrorMessage { get; set; }

    /// <summary>Gets or sets whether this was a sandbox document.</summary>
    public bool IsSandbox { get; set; }

    /// <summary>Gets or sets when the document was originally created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the document was submitted.</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>Gets or sets when the document was validated.</summary>
    public DateTime? ValidatedAt { get; set; }

    /// <summary>Gets or sets when the document was cancelled.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Gets or sets the submission UID.</summary>
    [MaxLength(32)]
    public string? SubmissionUid { get; set; }

    /// <summary>Gets or sets when this record was archived.</summary>
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
}
