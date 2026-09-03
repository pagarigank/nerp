// <copyright file="MyInvoisApiClient.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ERP.Modules.EInvoice.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

public class MyInvoisApiClient : IMyInvoisApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MyInvoisApiClient> _logger;

    public MyInvoisApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<MyInvoisApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetBaseUrl()
    {
        var environment = _configuration["EInvoice:Environment"]?.ToLowerInvariant() ?? "sandbox";
        return environment == "production"
            ? "https://api.myinvois.hasil.gov.my"
            : "https://preprod-api.myinvois.hasil.gov.my";
    }

    private string GetClientId()
    {
        return _configuration["EInvoice:ClientId"]
            ?? throw new InvalidOperationException("EInvoice:ClientId is not configured. Set it in appsettings.json or user secrets.");
    }

    private string GetClientSecret()
    {
        return _configuration["EInvoice:ClientSecret"]
            ?? throw new InvalidOperationException("EInvoice:ClientSecret is not configured. Set it in appsettings.json or user secrets.");
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        var clientId = !string.IsNullOrEmpty(request.ClientId) ? request.ClientId : GetClientId();
        var clientSecret = !string.IsNullOrEmpty(request.ClientSecret) ? request.ClientSecret : GetClientSecret();

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        });

        var baseUrl = GetBaseUrl();
        _httpClient.BaseAddress = new Uri(baseUrl);

        var response = await _httpClient.PostAsync("/connect/token", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MyInvois authentication failed: {StatusCode} {Response}", response.StatusCode, json);
            throw new HttpRequestException($"Authentication failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<AuthenticationResponse>(json, JsonOptions)!;
    }

    /// <summary>
    /// Submits document(s) to MyInvois API.
    /// Enforces LHDN limits: max 100 documents per batch, max 5MB total payload.
    /// </summary>
    public async Task<SubmissionResponse> SubmitDocumentsAsync(SubmissionRequest request, string accessToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Validate single-document size (300KB limit per LHDN)
        var payloadBytes = Encoding.UTF8.GetByteCount(request.Payload);
        const int maxDocSize = 300 * 1024; // 300KB
        if (payloadBytes > maxDocSize)
        {
            _logger.LogWarning("Document payload exceeds 300KB limit: {Size} bytes", payloadBytes);
            throw new ArgumentException($"Document payload exceeds 300KB limit ({payloadBytes:N0} bytes)");
        }

        var json = JsonSerializer.Serialize(new
        {
            documentType = request.DocumentType,
            payload = request.Payload,
            format = request.Format
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Validate total batch size (5MB limit per LHDN)
        var batchBytes = Encoding.UTF8.GetByteCount(json);
        const int maxBatchSize = 5 * 1024 * 1024; // 5MB
        if (batchBytes > maxBatchSize)
        {
            _logger.LogWarning("Batch payload exceeds 5MB limit: {Size} bytes", batchBytes);
            throw new ArgumentException($"Batch payload exceeds 5MB limit ({batchBytes:N0} bytes)");
        }

        _logger.LogInformation("Submitting document to MyInvois: type={DocumentType}, size={Size} bytes", request.DocumentType, batchBytes);

        var response = await _httpClient.PostAsync("/api/v1.0/documentsubmissions", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MyInvois submission failed: {StatusCode} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"Submission failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<SubmissionResponse>(responseJson, JsonOptions)!;
    }

    public async Task<SubmissionStatusResponse> GetSubmissionStatusAsync(string submissionUid, string accessToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync($"/api/v1.0/documentsubmissions/{submissionUid}", cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MyInvois status poll failed: {StatusCode} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"Status poll failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<SubmissionStatusResponse>(responseJson, JsonOptions)!;
    }

    public async Task<TinValidationResponse> ValidateTaxpayerTinAsync(string tin, string accessToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync($"/api/v1.0/tin/{tin}", cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("TIN validation failed: {StatusCode} {Response}", response.StatusCode, responseJson);
            return new TinValidationResponse(false);
        }

        return JsonSerializer.Deserialize<TinValidationResponse>(responseJson, JsonOptions)!;
    }

    public async Task<CancelResponse> CancelDocumentAsync(string uuid, string reason, string accessToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = JsonSerializer.Serialize(new { reason });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/v1.0/documents/{uuid}/cancel", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Document cancellation failed: {StatusCode} {Response}", response.StatusCode, responseJson);
            throw new HttpRequestException($"Cancellation failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<CancelResponse>(responseJson, JsonOptions)!;
    }
}