// <copyright file="EInvoiceStatusHistory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.Modules.EInvoice.Domain;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// Audit trail entry for e-Invoice document status transitions.
/// Records every state change with who/when/what for the 7-year statutory retention.
/// </summary>
[Table("einvoice_status_history", Schema = "int")]
public class EInvoiceStatusHistory
{
    public EInvoiceStatusHistory()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets or sets the unique ID of this history entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the e-Invoice document this history entry belongs to.
    /// </summary>
    public Guid EInvoiceDocumentId { get; set; }

    /// <summary>
    /// Gets or sets the status before the transition.
    /// Null for the initial creation entry.
    /// </summary>
    [MaxLength(50)]
    public string? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status after the transition.
    /// </summary>
    [Required, MaxLength(50)]
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who initiated the status change.
    /// "system" for automated changes (polling, submission).
    /// </summary>
    [Required, MaxLength(100)]
    public string ActorUserId { get; set; } = "system";

    /// <summary>
    /// Gets or sets a human-readable description of why the status changed.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the error code from LHDN (if the status is Invalid/Rejected).
    /// </summary>
    [MaxLength(50)]
    public string? LhdnErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message from LHDN (if the status is Invalid/Rejected).
    /// </summary>
    [MaxLength(500)]
    public string? LhdnErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the MyInvois UUID (if assigned during this transition).
    /// </summary>
    [MaxLength(100)]
    public string? MyInvoisUuid { get; set; }

    /// <summary>
    /// Gets or sets the validation link from LHDN (if assigned).
    /// </summary>
    [MaxLength(500)]
    public string? ValidationLink { get; set; }

    /// <summary>
    /// Gets or sets when this status change occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Creates a history entry for a status transition.
    /// </summary>
    public static EInvoiceStatusHistory Create(
        Guid documentId,
        string? previousStatus,
        string newStatus,
        string actorUserId,
        string? reason = null,
        string? lhdnErrorCode = null,
        string? lhdnErrorMessage = null,
        string? myInvoisUuid = null,
        string? validationLink = null)
    {
        return new EInvoiceStatusHistory
        {
            EInvoiceDocumentId = documentId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActorUserId = actorUserId ?? "system",
            Reason = reason,
            LhdnErrorCode = lhdnErrorCode,
            LhdnErrorMessage = lhdnErrorMessage,
            MyInvoisUuid = myInvoisUuid,
            ValidationLink = validationLink,
            OccurredAt = DateTime.UtcNow,
        };
    }
}
