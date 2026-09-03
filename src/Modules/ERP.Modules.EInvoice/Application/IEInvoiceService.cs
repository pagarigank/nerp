// <copyright file="IEInvoiceService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Domain;

namespace ERP.Modules.EInvoice.Application;

public interface IEInvoiceService
{
    Task<EInvoiceSubmissionResult> SubmitDocumentAsync(
        Guid companyId,
        Guid sourceDocumentId,
        string documentType,
        string payload,
        string? tin = null,
        CancellationToken cancellationToken = default);

    Task<EInvoiceDocumentStatusDto> GetDocumentStatusAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<EInvoicePollingResult> PollSubmissionStatusAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<EInvoiceValidationResult> ValidateTinAsync(string tin, CancellationToken cancellationToken = default);

    Task<bool> CancelDocumentAsync(Guid documentId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resubmits a failed/invalid e-Invoice document as a new submission.
    /// Creates a new document record, copies corrected payload, and submits to MyInvois.
    /// </summary>
    Task<EInvoiceSubmissionResult> ResubmitAsync(
        Guid originalDocumentId,
        string correctedPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a buyer-side rejection of a validated e-Invoice.
    /// The buyer rejects the invoice within the allowed window, requiring a credit note.
    /// </summary>
    Task<bool> RecordBuyerRejectionAsync(
        Guid documentId,
        string rejectionReason,
        CancellationToken cancellationToken = default);
}