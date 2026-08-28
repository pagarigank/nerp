// <copyright file="CursorPagination.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, CA1000, S1118
#pragma warning disable S2743

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.Api.Performance;

/// <summary>
/// Cursor-based pagination for large result sets using base64-encoded JSON cursor tokens.
/// Avoids the "deep offset" performance problem of traditional OFFSET/FETCH.
/// </summary>
public sealed class CursorPagination<T>
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Create a cursor from a sort value (e.g., a DateTime, Guid, or string).
    /// </summary>
    public static string CreateCursor(T sortValue)
    {
        var json = JsonSerializer.Serialize(new CursorPayload<T> { SortValue = sortValue, Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, JsonOptions);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decode a cursor token back to the sort value.
    /// Returns default if the cursor is invalid or expired (>24 hours old).
    /// </summary>
    public static (T? SortValue, bool IsValid) DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return (default, true);

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var payload = JsonSerializer.Deserialize<CursorPayload<T>>(json, JsonOptions);
            if (payload == null)
                return (default, false);

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - payload.Ts > 86400)
                return (default, false);

            return (payload.SortValue, true);
        }
        catch
        {
            return (default, false);
        }
    }

    /// <summary>
    /// Parse page size from query string, clamped to [1, MaxPageSize].
    /// </summary>
    public static int ParsePageSize(int? requested)
    {
        if (!requested.HasValue || requested.Value <= 0)
            return DefaultPageSize;
        return Math.Min(requested.Value, MaxPageSize);
    }

    /// <summary>
    /// Build the paged response with cursor metadata.
    /// </summary>
    public static CursorPagedResult<TResult> ToCursorResult<TResult>(
        IReadOnlyList<TResult> items,
        int pageSize,
        string? nextCursor,
        int? totalCount = null)
    {
        return new CursorPagedResult<TResult>
        {
            Items = items,
            NextCursor = nextCursor,
            PageSize = pageSize,
            HasMore = items.Count == pageSize,
            TotalCount = totalCount,
        };
    }
}

public sealed class CursorPayload<T>
{
    [JsonPropertyName("sortValue")]
    public T? SortValue { get; set; }

    [JsonPropertyName("ts")]
    public long Ts { get; set; }
}

public sealed class CursorPagedResult<T>
{
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = [];

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }

    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }
}
