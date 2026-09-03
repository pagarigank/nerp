// <copyright file="EInvoiceDtos.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;

namespace ERP.Modules.EInvoice.Domain;

public class EInvoiceSubmissionResult
{
    public bool Success { get; set; }
    public string? Uuid { get; set; }
    public string? SubmissionUid { get; set; }
    public string? Error { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsDuplicate { get; set; } = false;
}

public class EInvoicePollingResult
{
    public bool IsComplete { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ValidationLink { get; set; }
    public string? ErrorMessage { get; set; }
}

public class EInvoiceDocumentStatusDto
{
    public Guid DocumentId { get; set; }
    public Guid SourceDocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MyInvoisUuid { get; set; }
    public string? ValidationLink { get; set; }
    public string? LhdnsErrorMessage { get; set; }
    public string? Error { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
}

public class EInvoiceValidationResult
{
    public bool IsValid { get; set; }
    public string? EntityName { get; set; }
    public string? EntityType { get; set; }
    public string? BusinessName { get; set; }
    public string? Address { get; set; }
    public DateTime? ValidatedAt { get; set; } = DateTime.UtcNow;
}