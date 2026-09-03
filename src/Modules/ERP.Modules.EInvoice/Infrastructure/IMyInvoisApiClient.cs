// <copyright file="IMyInvoisApiClient.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Infrastructure;

public interface IMyInvoisApiClient
{
    Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> SubmitDocumentsAsync(SubmissionRequest request, string accessToken, CancellationToken cancellationToken = default);

    Task<SubmissionStatusResponse> GetSubmissionStatusAsync(string submissionUid, string accessToken, CancellationToken cancellationToken = default);

    Task<TinValidationResponse> ValidateTaxpayerTinAsync(string tin, string accessToken, CancellationToken cancellationToken = default);

    Task<CancelResponse> CancelDocumentAsync(string uuid, string reason, string accessToken, CancellationToken cancellationToken = default);
}

public record AuthenticationRequest(string ClientId, string ClientSecret, string Environment);
public record AuthenticationResponse(string AccessToken, int ExpiresIn, string TokenType);

public record SubmissionRequest(string DocumentType, string Payload, string Format = "JSON");

public record SubmissionResponse(string SubmissionUid, string Status, List<DocumentResult> Results);

public record DocumentResult(string Uuid, string Status, string? ErrorMessage);

public record SubmissionStatusResponse(string SubmissionUid, string Status, List<DocumentStatus> Documents);

public record DocumentStatus(string Uuid, string Status, DateTime? ValidatedOn, string? ErrorMessage);

public record TinValidationResponse(bool IsValid, string? EntityName = null, string? EntityType = null, string? BusinessName = null, string? Address = null);

public record CancelResponse(string Uuid, string Status, DateTime? ValidatedOn);