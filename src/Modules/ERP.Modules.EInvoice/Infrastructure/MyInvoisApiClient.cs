// <copyright file="MyInvoisApiClient.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ERP.Modules.EInvoice.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace ERP.Modules.EInvoice.Infrastructure;

public class MyInvoisApiClient : IMyInvoisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public Action<string, Exception?> LogError { get; set; } = (_, __) => { };

    public MyInvoisApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", request.ClientId),
            new KeyValuePair<string, string>("client_secret", request.ClientSecret)
        });

        var environment = request.Environment?.ToLowerInvariant() == "production"
            ? "prod"
            : "sandbox";
        var baseUrl = environment == "production"
            ? "https://api.myinvois.hasil.gov.my"
            : "https://preprod-api.myinvois.hasil.gov.my";

        _httpClient.BaseAddress = new Uri(baseUrl);

        var response = await _httpClient.PostAsync("/connect/token", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogError($"MyInvois authentication failed: {response.StatusCode} {json}", null);
            throw new HttpRequestException($"Authentication failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<AuthenticationResponse>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    public async Task<SubmissionResponse> SubmitDocumentsAsync(SubmissionRequest request, string accessToken, CancellationToken cancellationToken = default)
    {
        var environment = _configuration["EInvoice:Environment"]?.ToLowerInvariant() == "production"
            ? "prod"
            : "sandbox";
        var baseUrl = environment == "production"
            ? "https://api.myinvois.hasil.gov.my"
            : "https://preprod-api.myinvois.hasil.gov.my";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = JsonSerializer.Serialize(new
        {
            documentType = request.DocumentType,
            payload = request.Payload,
            format = request.Format
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/v1.0/documentsubmissions", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogError($"MyInvois submission failed: {response.StatusCode} {responseJson}", null);
            throw new HttpRequestException($"Submission failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<SubmissionResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    public async Task<SubmissionStatusResponse> GetSubmissionStatusAsync(string submissionUid, string accessToken, CancellationToken cancellationToken = default)
    {
        var environment = _configuration["EInvoice:Environment"]?.ToLowerInvariant() == "production"
            ? "prod"
            : "sandbox";
        var baseUrl = environment == "production"
            ? "https://api.myinvois.hasil.gov.my"
            : "https://preprod-api.myinvois.hasil.gov.my";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync($"/api/v1.0/documentsubmissions/{submissionUid}", cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogError($"MyInvois status poll failed: {response.StatusCode} {responseJson}", null);
            throw new HttpRequestException($"Status poll failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<SubmissionStatusResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    public async Task<TinValidationResponse> ValidateTaxpayerTinAsync(string tin, string accessToken, CancellationToken cancellationToken = default)
    {
        var environment = _configuration["EInvoice:Environment"]?.ToLowerInvariant() == "production"
            ? "prod"
            : "sandbox";
        var baseUrl = environment == "production"
            ? "https://api.myinvois.hasil.gov.my"
            : "https://preprod-api.myinvois.hasil.gov.my";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync($"/api/v1.0/tin/{tin}", cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogError($"TIN validation failed: {response.StatusCode} {responseJson}", null);
            return new TinValidationResponse(false);
        }

        return JsonSerializer.Deserialize<TinValidationResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }

    public async Task<CancelResponse> CancelDocumentAsync(string uuid, string reason, string accessToken, CancellationToken cancellationToken = default)
    {
        var environment = _configuration["EInvoice:Environment"]?.ToLowerInvariant() == "production"
            ? "prod"
            : "sandbox";
        var baseUrl = environment == "production"
            ? "https://api.myinvois.hasil.gov.my"
            : "https://preprod-api.myinvois.hasil.gov.my";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = JsonSerializer.Serialize(new { reason });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/v1.0/documents/{uuid}/cancel", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogError($"Document cancellation failed: {response.StatusCode} {responseJson}", null);
            throw new HttpRequestException($"Cancellation failed: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<CancelResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }
}