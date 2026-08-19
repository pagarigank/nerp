using System.Text.Json.Serialization;

namespace ERP.Core.Common;

public record PagedResult<T>
{
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("totalCount")]
    public long TotalCount { get; init; }

    [JsonPropertyName("totalPages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalCount) =>
        new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}