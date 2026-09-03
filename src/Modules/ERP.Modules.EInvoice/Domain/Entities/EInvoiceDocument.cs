// <copyright file="EInvoiceDocument.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.Modules.EInvoice.Domain;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// e-Invoice document record.
/// Tracks document lifecycle: Queued -> Signed -> Submitted -> Valid/Invalid -> Cancelled/Rejected
/// </summary>
[Table("einvoice_document", Schema = "int")]
public class EInvoiceDocument
{
    public EInvoiceDocument()
    {
        Id = Guid.NewGuid();
        Status = EInvoiceDocumentStatus.Queued;
    }

    [Key]
    public Guid Id { get; set; }

    // ERP source reference
    [Required]
    public Guid SourceDocumentId { get; set; }

    [Required, MaxLength(50)]
    public string SourceDocumentType { get; set; } = string.Empty; // Invoice, CreditNote, DebitNote, RefundNote, SelfBilledInvoice

    // Document info
    [Required, MaxLength(50)]
    public string DocumentType { get; set; } = "Invoice";

    [Required, MaxLength(18)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0m;

    [MaxLength(50)]
    public string? TaxCodeId { get; set; }

    [Required, MaxLength(3)]
    public string CurrencyCode { get; set; } = "MYR";

    // Supplier/Customer TIN for reconciliation
    [MaxLength(50)]
    public string? SupplierTIN { get; set; }

    [MaxLength(50)]
    public string? CustomerTIN { get; set; }

    // MyInvois response
    [MaxLength(100)]
    public string? MyInvoisUuid { get; set; }

    [MaxLength(500)]
    public string? ValidationLink { get; set; }

    // QR code data (base64 or raw JSON)
    public string? QrCodePayload { get; set; }

    // Status tracking
    public EInvoiceDocumentStatus Status { get; set; }

    [MaxLength(50)]
    public string? LhdnsErrorCode { get; set; }

    [MaxLength(500)]
    public string? LhdnsErrorMessage { get; set; }

    // Environment
    public bool IsSandbox { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SignedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ValidatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Metadata
    [MaxLength(32)]
    public string? SubmissionUid { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; } // JSON or XML

    [MaxLength(64)]
    public string? ContentHash { get; set; }

    // For self-billed invoices, reference to the original invoice
    [MaxLength(64)]
    public string? SelfBilledInvoiceId { get; set; }

    // Audit
    [MaxLength(100)]
    public string? CreatedByUserId { get; set; }

    [MaxLength(100)]
    public string? UpdatedByUserId { get; set; }
}