// <copyright file="EInvoiceService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.EInvoice.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Modules.EInvoice.Infrastructure;

public class EInvoiceService : IEInvoiceService
{
    private readonly IMyInvoisApiClient _myInvoisClient;
    private readonly EInvoiceDbContext _dbContext;
    private readonly IMyInvoisTokenCache _tokenCache;
    private readonly IConfiguration _configuration;
    private readonly IQRCodeService _qrCodeService;

    public EInvoiceService(
        IMyInvoisApiClient myInvoisClient,
        EInvoiceDbContext dbContext,
        IMyInvoisTokenCache tokenCache,
        IConfiguration configuration,
        IQRCodeService qrCodeService)
    {
        _myInvoisClient = myInvoisClient;
        _dbContext = dbContext;
        _tokenCache = tokenCache;
        _configuration = configuration;
        _qrCodeService = qrCodeService;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var clientId = _configuration["EInvoice:ClientId"] ?? string.Empty;
        var clientSecret = _configuration["EInvoice:ClientSecret"] ?? string.Empty;
        var environment = _configuration["EInvoice:Environment"] ?? "sandbox";
        return await _tokenCache.GetAccessTokenAsync(clientId, clientSecret, environment, ct);
    }

    public async Task<EInvoiceSubmissionResult> SubmitDocumentAsync(
        Guid companyId,
        Guid sourceDocumentId,
        string documentType,
        string payload,
        string? tin = null,
        CancellationToken cancellationToken = default)
    {
        // Hash-based idempotency check (scoped to company)
        var contentHash = ComputeContentHash(payload);
        var existingDoc = await _dbContext.EInvoiceDocuments
            .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.ContentHash == contentHash && d.Status != EInvoiceDocumentStatus.Cancelled, cancellationToken);
        
        if (existingDoc != null)
        {
            return new EInvoiceSubmissionResult
            {
                Success = true,
                Uuid = existingDoc.MyInvoisUuid,
                SubmissionUid = existingDoc.SubmissionUid,
                DocumentId = existingDoc.Id,
                IsDuplicate = true
            };
        }

        // Get cached access token (auto-refreshes if expired)
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        // Validate batch size before submission
        var payloadBytes = System.Text.Encoding.UTF8.GetByteCount(payload);
        if (payloadBytes > 300 * 1024)
        {
            return new EInvoiceSubmissionResult
            {
                Success = false,
                Error = $"Document payload exceeds 300KB limit ({payloadBytes:N0} bytes)"
            };
        }

        // Submit document
        var submitRequest = new SubmissionRequest(documentType, payload, "JSON");
        var response = await _myInvoisClient.SubmitDocumentsAsync(submitRequest, accessToken, cancellationToken);

        var result = new EInvoiceSubmissionResult
        {
            Success = response.Status == "Accepted",
            Uuid = response.Results.FirstOrDefault()?.Uuid,
            SubmissionUid = response.SubmissionUid,
            Error = response.Results.FirstOrDefault()?.ErrorMessage
        };

        // Store the document in DB
        var firstResult = response.Results.FirstOrDefault();
        if (firstResult != null)
        {
            var dbDocument = new EInvoiceDocument
            {
                CompanyId = companyId,
                SourceDocumentId = sourceDocumentId,
                DocumentType = documentType,
                TotalAmount = ParseAmount(payload),
                Status = EInvoiceDocumentStatus.Submitted,
                MyInvoisUuid = firstResult.Uuid,
                SubmissionUid = response.SubmissionUid,
                ContentType = "JSON",
                ContentHash = contentHash,
                IsSandbox = false,
                SubmittedAt = DateTime.UtcNow
            };
            
            _dbContext.EInvoiceDocuments.Add(dbDocument);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            result.DocumentId = dbDocument.Id;
        }

        return result;
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static decimal ParseAmount(string jsonPayload)
    {
        if (jsonPayload.Contains("\"totalAmount\"", StringComparison.Ordinal))
        {
            var startIdx = jsonPayload.IndexOf("\"totalAmount\"", StringComparison.Ordinal) + 15;
            var endIdx = jsonPayload.IndexOf('}', startIdx);
            if (endIdx == -1) endIdx = jsonPayload.IndexOf(',', startIdx);
            var amountStr = jsonPayload.Substring(startIdx, endIdx - startIdx).Trim();
            if (decimal.TryParse(amountStr, out var amount))
                return amount;
        }
        return 0m;
    }

    public async Task<EInvoiceDocumentStatusDto> GetDocumentStatusAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.EInvoiceDocuments.FindAsync([documentId], cancellationToken);
        if (document == null)
            throw new InvalidOperationException($"Document {documentId} not found");

        return new EInvoiceDocumentStatusDto
        {
            DocumentId = document.Id,
            SourceDocumentId = document.SourceDocumentId,
            DocumentType = document.DocumentType,
            Status = document.Status.ToString(),
            MyInvoisUuid = document.MyInvoisUuid,
            ValidationLink = document.ValidationLink,
            LhdnsErrorMessage = document.LhdnsErrorMessage,
            SubmittedAt = document.SubmittedAt,
            ValidatedAt = document.ValidatedAt
        };
    }

    private async Task RecordStatusChange(
        Guid documentId,
        string? previousStatus,
        string newStatus,
        string actorUserId,
        string? reason = null,
        string? lhdnErrorCode = null,
        string? lhdnErrorMessage = null,
        string? myInvoisUuid = null,
        string? validationLink = null,
        CancellationToken ct = default)
    {
        var history = EInvoiceStatusHistory.Create(documentId, previousStatus, newStatus, actorUserId, reason, lhdnErrorCode, lhdnErrorMessage, myInvoisUuid, validationLink);
        _dbContext.EInvoiceStatusHistories.Add(history);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<EInvoicePollingResult> PollSubmissionStatusAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.EInvoiceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        
        if (document == null)
            throw new InvalidOperationException($"Document {documentId} not found");

        if (string.IsNullOrEmpty(document.SubmissionUid))
            throw new InvalidOperationException("Document has not been submitted");

        // Get cached access token
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var statusResponse = await _myInvoisClient.GetSubmissionStatusAsync(
            document.SubmissionUid,
            accessToken,
            cancellationToken);

        var docStatus = statusResponse.Documents.FirstOrDefault();
        var previousStatus = document.Status.ToString();
        var newStatus = docStatus != null ? MapStatus(docStatus.Status) : document.Status;
        document.Status = newStatus;
        document.ValidatedAt = DateTime.UtcNow;

        // Store UUID and validation link from MyInvois response
        if (!string.IsNullOrEmpty(docStatus?.Uuid))
        {
            document.MyInvoisUuid = docStatus.Uuid;
        }

        if (!string.IsNullOrEmpty(docStatus?.ErrorMessage))
        {
            document.LhdnsErrorMessage = LhdnErrorMapper.MapError(null, docStatus.ErrorMessage);
        }

        // Generate QR payload when document becomes valid
        if (newStatus == EInvoiceDocumentStatus.Valid && !string.IsNullOrEmpty(document.MyInvoisUuid))
        {
            document.ValidationLink = $"https://myinvois.hasil.gov.my/{document.MyInvoisUuid}";
            try
            {
                await _qrCodeService.GenerateQrPayloadAsync(documentId, cancellationToken);
            }
            catch (Exception)
            {
                // QR generation failure is non-fatal — log and continue
                // The document is still valid even without QR payload
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Record audit trail
        await RecordStatusChange(documentId, previousStatus, newStatus.ToString(), "system-polling", lhdnErrorCode: null, lhdnErrorMessage: docStatus?.ErrorMessage, myInvoisUuid: docStatus?.Uuid, ct: cancellationToken);

        return new EInvoicePollingResult
        {
            IsComplete = document.Status == EInvoiceDocumentStatus.Valid || 
                         document.Status == EInvoiceDocumentStatus.Invalid ||
                         document.Status == EInvoiceDocumentStatus.Cancelled,
            Status = document.Status.ToString(),
            ErrorMessage = document.LhdnsErrorMessage
        };
    }

    private static EInvoiceDocumentStatus MapStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "accepted" => EInvoiceDocumentStatus.Valid,
            "invalid" => EInvoiceDocumentStatus.Invalid,
            "cancelled" => EInvoiceDocumentStatus.Cancelled,
            "rejected" => EInvoiceDocumentStatus.Rejected,
            _ => EInvoiceDocumentStatus.Submitted
        };
    }

    public async Task<EInvoiceValidationResult> ValidateTinAsync(string tin, CancellationToken cancellationToken = default)
    {
        // Get cached access token
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var response = await _myInvoisClient.ValidateTaxpayerTinAsync(tin, accessToken, cancellationToken);

        return new EInvoiceValidationResult
        {
            IsValid = response.IsValid,
            EntityName = response.EntityName,
            EntityType = response.EntityType,
            BusinessName = response.BusinessName,
            Address = response.Address
        };
    }

    public async Task<bool> CancelDocumentAsync(Guid documentId, string reason, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.EInvoiceDocuments.FindAsync(documentId);
        if (document == null)
            return false;

        if (!EInvoiceComplianceHelper.CanCancelDocument(document.SubmittedAt ?? DateTime.UtcNow, DateTime.UtcNow))
            throw new InvalidOperationException("Cancellation window has expired (72 hours from submission)");

        if (string.IsNullOrEmpty(document.SubmissionUid))
            throw new InvalidOperationException("Document not yet submitted");

        // Get cached access token
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var cancelResult = await _myInvoisClient.CancelDocumentAsync(
            document.SubmissionUid,
            reason,
            accessToken,
            cancellationToken);

        if (cancelResult.Status == "Accepted" || cancelResult.Status == "Cancelled")
        {
            var previousStatus = document.Status.ToString();
            document.Status = EInvoiceDocumentStatus.Cancelled;
            document.CancelledAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Record audit trail with cancellation reason
            await RecordStatusChange(documentId, previousStatus, EInvoiceDocumentStatus.Cancelled.ToString(), "user", reason: reason, ct: cancellationToken);
        }

        return document.Status == EInvoiceDocumentStatus.Cancelled;
    }
}