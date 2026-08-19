using System.Text.Json.Serialization;
using Ardalis.Result;

namespace ERP.Shared.Kernel.Api;

public sealed record ApiResponse<T>
{
    public static ApiResponse<T> Success(T data, string? message = null, string? traceId = null) =>
        new()
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            TraceId = traceId
        };

    public static ApiResponse<T> Failure(IEnumerable<string> errors, int? statusCode = null, string? traceId = null) =>
        new()
        {
            IsSuccess = false,
            Errors = errors.ToList(),
            StatusCode = statusCode,
            TraceId = traceId
        };

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; init; } = [];

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }
}

public sealed record ApiResponse
{
    public static ApiResponse Success(string? message = null, string? traceId = null) =>
        new()
        {
            IsSuccess = true,
            Message = message,
            TraceId = traceId
        };

    public static ApiResponse Failure(IEnumerable<string> errors, int? statusCode = null, string? traceId = null) =>
        new()
        {
            IsSuccess = false,
            Errors = errors.ToList(),
            StatusCode = statusCode,
            TraceId = traceId
        };

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; init; } = [];

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }
}

public sealed record ProblemDetailsResponse
{
    public string Type { get; init; } = "about:blank";
    public string Title { get; init; } = "An error occurred";
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
    public string? TraceId { get; init; }

    public static ProblemDetailsResponse FromException(Exception ex, int statusCode, string? traceId = null) =>
        new()
        {
            Title = ex.GetType().Name,
            Status = statusCode,
            Detail = ex.Message,
            TraceId = traceId
        };

    public static ProblemDetailsResponse ValidationFailed(IEnumerable<KeyValuePair<string, string[]>> errors, string? traceId = null) =>
        new()
        {
            Title = "Validation Failed",
            Status = 400,
            Detail = "One or more validation errors occurred",
            Errors = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            TraceId = traceId
        };
}

public static class ResultExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result, string? traceId = null)
    {
        return result.IsSuccess
            ? ApiResponse<T>.Success(result.Value!, traceId: traceId)
            : ApiResponse<T>.Failure(result.Errors, result.Status.GetHttpStatusCode(), traceId);
    }

    public static ApiResponse ToApiResponse(this Result result, string? traceId = null)
    {
        return result.IsSuccess
            ? ApiResponse.Success(traceId: traceId)
            : ApiResponse.Failure(result.Errors, result.Status.GetHttpStatusCode(), traceId);
    }

    private static int GetHttpStatusCode(this ResultStatus status)
    {
        return status switch
        {
            ResultStatus.Invalid => 400,
            ResultStatus.NotFound => 404,
            ResultStatus.Unauthorized => 401,
            ResultStatus.Forbidden => 403,
            ResultStatus.Conflict => 409,
            ResultStatus.Error => 500,
            _ => 500
        };
    }
}
