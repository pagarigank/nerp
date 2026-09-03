// <copyright file="EInvoiceService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.EInvoice.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.EInvoice.Infrastructure;

public class EInvoiceService : IEInvoiceService
{
    private readonly IMyInvoisApiClient _myInvoisClient;
    private readonly EInvoiceDbContext _dbContext;

    public EInvoiceService(IMyInvoisApiClient myInvoisClient, EInvoiceDbContext dbContext)
    {
        _myInvoisClient = myInvoisClient;
        _dbContext = dbContext;
    }

    public async Task<EInvoiceSubmissionResult> SubmitDocumentAsync(
        Guid companyId,
        Guid sourceDocumentId,
        string documentType,
        string payload,
        string? tin = null,
        CancellationToken cancellationToken = default)
    {
        // Hash-based idempotency check
        var contentHash = ComputeContentHash(payload);
        var existingDoc = await _dbContext.EInvoiceDocuments
            .FirstOrDefaultAsync(d => d.ContentHash == contentHash && d.Status != EInvoiceDocumentStatus.Cancelled, cancellationToken);
        
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

        // Authenticate with MyInvois
        var authRequest = new AuthenticationRequest(
            ClientId: string.Empty,
            ClientSecret: string.Empty,
            Environment: "production"
        );

        var authResponse = await _myInvoisClient.AuthenticateAsync(authRequest, cancellationToken);

        // Submit document
        var submitRequest = new SubmissionRequest(documentType, payload, "JSON");
        var response = await _myInvoisClient.SubmitDocumentsAsync(submitRequest, authResponse.AccessToken, cancellationToken);

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
                SourceDocumentId = sourceDocumentId,
                DocumentType = documentType,
                TotalAmount = ParseAmount(payload),
                Status = EInvoiceDocumentStatus.Submitted,
                MyInvoisUuid = firstResult.Uuid,
                SubmissionUid = response.SubmissionUid,
                ContentType = "JSON",
                ContentHash = contentHash,
                IsSandbox = false
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

        var authRequest = new AuthenticationRequest(
            ClientId: string.Empty,
            ClientSecret: string.Empty,
            Environment: "production"
        );

        var authResponse = await _myInvoisClient.AuthenticateAsync(authRequest, cancellationToken);
        var statusResponse = await _myInvoisClient.GetSubmissionStatusAsync(
            document.SubmissionUid,
            authResponse.AccessToken,
            cancellationToken);

        var docStatus = statusResponse.Documents.FirstOrDefault();
        document.Status = docStatus != null ? MapStatus(docStatus.Status) : document.Status;
        document.ValidatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(docStatus?.ErrorMessage))
        {
            document.LhdnsErrorMessage = docStatus.ErrorMessage;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

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
        var cert = await _dbContext.DigitalCertificates.FirstOrDefaultAsync(cancellationToken);
        if (cert == null)
            throw new InvalidOperationException("No certificate configured");

        var authRequest = new AuthenticationRequest(
            ClientId: string.Empty,
            ClientSecret: string.Empty,
            Environment: "production"
        );

        var authResponse = await _myInvoisClient.AuthenticateAsync(authRequest, cancellationToken);
        var response = await _myInvoisClient.ValidateTaxpayerTinAsync(tin, authResponse.AccessToken, cancellationToken);

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

        var authRequest = new AuthenticationRequest(
            ClientId: string.Empty,
            ClientSecret: string.Empty,
            Environment: "production"
        );

        var authResponse = await _myInvoisClient.AuthenticateAsync(authRequest, cancellationToken);
        var cancelResult = await _myInvoisClient.CancelDocumentAsync(
            document.SubmissionUid,
            reason,
            authResponse.AccessToken,
            cancellationToken);

        if (cancelResult.Status == "Accepted" || cancelResult.Status == "Cancelled")
        {
            document.Status = EInvoiceDocumentStatus.Cancelled;
            document.CancelledAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return document.Status == EInvoiceDocumentStatus.Cancelled;
    }
}