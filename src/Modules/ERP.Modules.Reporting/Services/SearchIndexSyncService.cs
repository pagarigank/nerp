// <copyright file="SearchIndexSyncService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using ERP.Core.Domain.Common;
using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Reporting.Services;

/// <summary>
/// Maintains a full-text search index for the report catalog. The index
/// covers report definitions, saved queries, financial statement layouts,
/// and quick queries. It supports:
/// - Incremental indexing (only re-index changed records since last sync)
/// - Full re-index (for disaster recovery or schema changes)
/// - Keyword search with relevance ranking
/// - Faceted search by module, category, and report type
/// - Search analytics (popular queries, zero-result queries)
/// </summary>
public interface ISearchIndexSyncService
{
    /// <summary>
    /// Performs an incremental sync of the search index. Only records
    /// modified since the last sync are re-indexed.
    /// </summary>
    Task<int> IncrementalSyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the entire search index from scratch.
    /// </summary>
    Task<int> FullReindexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the index with the given query string. Returns results
    /// ranked by relevance, with facets for module/category/type.
    /// </summary>
    Task<SearchResult> SearchAsync(
        string query,
        string? moduleFilter = null,
        string? categoryFilter = null,
        int maxResults = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns search analytics: popular queries, zero-result queries,
    /// and usage by module.
    /// </summary>
    Task<SearchAnalytics> GetAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a search query for analytics purposes.
    /// </summary>
    Task RecordSearchAsync(string query, int resultCount, CancellationToken cancellationToken = default);
}

public class SearchResult
{
    public string Query { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public IReadOnlyList<SearchResultItem> Items { get; set; } = [];
    public SearchFacets Facets { get; set; } = new();
}

public class SearchResultItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string Snippet { get; set; } = string.Empty;
}

public class SearchFacets
{
    public IReadOnlyList<FacetBucket> Modules { get; set; } = [];
    public IReadOnlyList<FacetBucket> Categories { get; set; } = [];
    public IReadOnlyList<FacetBucket> Types { get; set; } = [];
}

public class FacetBucket
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SearchAnalytics
{
    public int TotalSearches { get; set; }
    public IReadOnlyList<PopularQuery> PopularQueries { get; set; } = [];
    public IReadOnlyList<ZeroResultQuery> ZeroResultQueries { get; set; } = [];
    public IReadOnlyList<FacetBucket> SearchesByModule { get; set; } = [];
}

public class PopularQuery
{
    public string Query { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AvgResultCount { get; set; }
}

public class ZeroResultQuery
{
    public string Query { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTimeOffset LastSeenOn { get; set; }
}

public class SearchIndexSyncService : ISearchIndexSyncService
{
    private readonly ReportingDbContext _rptDb;
    private readonly ILogger<SearchIndexSyncService> _logger;

    public SearchIndexSyncService(
        ReportingDbContext rptDb,
        ILogger<SearchIndexSyncService> logger)
    {
        _rptDb = rptDb ?? throw new ArgumentNullException(nameof(rptDb));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> IncrementalSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Search index incremental sync starting at {Time}", DateTimeOffset.UtcNow);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var lastSync = await _rptDb.SearchIndexSyncState
            .FirstOrDefaultAsync(s => s.StringId == "last-sync", cancellationToken);

        var cutoff = lastSync?.LastSyncOn ?? DateTimeOffset.MinValue;
        var indexedCount = 0;

        // Index report definitions modified since last sync
        var reports = await _rptDb.ReportDefinitions
            .Where(r => r.ModifiedOn >= cutoff || r.CreatedOn >= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var report in reports)
        {
            await UpsertSearchEntryAsync(report, cancellationToken);
            indexedCount++;
        }

        // Index saved queries
        var queries = await _rptDb.SavedQueries
            .Where(q => q.ModifiedOn >= cutoff || q.CreatedOn >= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var query in queries)
        {
            await UpsertSearchEntryAsync(query, cancellationToken);
            indexedCount++;
        }

        // Index financial statement layouts
        var layouts = await _rptDb.FinancialStatementLayouts
            .Where(l => l.ModifiedOn >= cutoff || l.CreatedOn >= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var layout in layouts)
        {
            await UpsertSearchEntryAsync(layout, cancellationToken);
            indexedCount++;
        }

        // Index quick queries
        var quickQueries = await _rptDb.QuickQueries
            .Where(q => q.ModifiedOn >= cutoff || q.CreatedOn >= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var qq in quickQueries)
        {
            await UpsertSearchEntryAsync(qq, cancellationToken);
            indexedCount++;
        }

        // Update sync state
        if (lastSync == null)
        {
            lastSync = new SearchIndexSyncState("last-sync");
            _rptDb.SearchIndexSyncState.Add(lastSync);
        }

        lastSync.RecordSync(indexedCount);

        await _rptDb.SaveChangesAsync(cancellationToken);

        sw.Stop();
        _logger.LogInformation(
            "Search index incremental sync completed. Indexed {Count} records in {Duration}ms",
            indexedCount, sw.ElapsedMilliseconds);

        return indexedCount;
    }

    public async Task<int> FullReindexAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Search index full reindex starting at {Time}", DateTimeOffset.UtcNow);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Clear existing index
        await _rptDb.SearchIndexEntries.ExecuteDeleteAsync(cancellationToken);
        await _rptDb.SaveChangesAsync(cancellationToken);

        var indexedCount = 0;

        // Index all report definitions
        var reports = await _rptDb.ReportDefinitions.ToListAsync(cancellationToken);
        foreach (var report in reports)
        {
            await UpsertSearchEntryAsync(report, cancellationToken);
            indexedCount++;
        }

        // Index all saved queries
        var queries = await _rptDb.SavedQueries.ToListAsync(cancellationToken);
        foreach (var query in queries)
        {
            await UpsertSearchEntryAsync(query, cancellationToken);
            indexedCount++;
        }

        // Index all financial statement layouts
        var layouts = await _rptDb.FinancialStatementLayouts.ToListAsync(cancellationToken);
        foreach (var layout in layouts)
        {
            await UpsertSearchEntryAsync(layout, cancellationToken);
            indexedCount++;
        }

        // Index all quick queries
        var quickQueries = await _rptDb.QuickQueries.ToListAsync(cancellationToken);
        foreach (var qq in quickQueries)
        {
            await UpsertSearchEntryAsync(qq, cancellationToken);
            indexedCount++;
        }

        // Update sync state
        var syncState = await _rptDb.SearchIndexSyncState
            .FirstOrDefaultAsync(s => s.StringId == "last-sync", cancellationToken);

        if (syncState == null)
        {
            syncState = new SearchIndexSyncState("last-sync");
            _rptDb.SearchIndexSyncState.Add(syncState);
        }

        syncState.RecordSync(indexedCount);

        await _rptDb.SaveChangesAsync(cancellationToken);

        sw.Stop();
        _logger.LogWarning(
            "Search index full reindex completed. Indexed {Count} records in {Duration}ms",
            indexedCount, sw.ElapsedMilliseconds);

        return indexedCount;
    }

    public async Task<SearchResult> SearchAsync(
        string query,
        string? moduleFilter = null,
        string? categoryFilter = null,
        int maxResults = 25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResult { Query = query, TotalCount = 0 };
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Build base query
        IQueryable<SearchIndexEntry> dbQuery = _rptDb.SearchIndexEntries;

        // Apply filters
        if (!string.IsNullOrEmpty(moduleFilter))
        {
            dbQuery = dbQuery.Where(e => e.Module == moduleFilter);
        }

        if (!string.IsNullOrEmpty(categoryFilter))
        {
            dbQuery = dbQuery.Where(e => e.Category == categoryFilter);
        }

        // Get all candidate entries
        var candidates = await dbQuery.ToListAsync(cancellationToken);

        // Score and rank
        var scored = candidates
            .Select(entry => new
            {
                Entry = entry,
                Score = ComputeRelevanceScore(entry, terms),
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .ToList();

        // Build facets from full result set (before take)
        var allScored = candidates
            .Select(entry => new { Entry = entry, Score = ComputeRelevanceScore(entry, terms) })
            .Where(x => x.Score > 0)
            .ToList();

        var facets = new SearchFacets
        {
            Modules = allScored
                .GroupBy(x => x.Entry.Module)
                .Select(g => new FacetBucket { Key = g.Key, Count = g.Count() })
                .OrderByDescending(b => b.Count)
                .ToList(),
            Categories = allScored
                .GroupBy(x => x.Entry.Category)
                .Select(g => new FacetBucket { Key = g.Key, Count = g.Count() })
                .OrderByDescending(b => b.Count)
                .ToList(),
            Types = allScored
                .GroupBy(x => x.Entry.ReportType)
                .Select(g => new FacetBucket { Key = g.Key, Count = g.Count() })
                .OrderByDescending(b => b.Count)
                .ToList(),
        };

        var items = scored.Select(x => new SearchResultItem
        {
            Id = x.Entry.SourceId,
            Title = x.Entry.Title,
            Description = x.Entry.Description ?? string.Empty,
            Module = x.Entry.Module,
            Category = x.Entry.Category,
            ReportType = x.Entry.ReportType,
            RelevanceScore = x.Score,
            Snippet = GenerateSnippet(x.Entry, terms),
        }).ToList();

        // Record search for analytics (fire-and-forget, don't fail the search)
        try
        {
            await RecordSearchAsync(query, allScored.Count, cancellationToken);
        }
        catch
        {
            // Analytics logging is non-critical
        }

        return new SearchResult
        {
            Query = query,
            TotalCount = allScored.Count,
            Items = items,
            Facets = facets,
        };
    }

    public async Task<SearchAnalytics> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var queries = await _rptDb.SearchQueryLogs
            .ToListAsync(cancellationToken);

        var popular = queries
            .GroupBy(q => q.Query.ToLowerInvariant())
            .Select(g => new PopularQuery
            {
                Query = g.Key,
                Count = g.Count(),
                AvgResultCount = g.Average(q => q.ResultCount),
            })
            .OrderByDescending(q => q.Count)
            .Take(20)
            .ToList();

        var zeroResults = queries
            .Where(q => q.ResultCount == 0)
            .GroupBy(q => q.Query.ToLowerInvariant())
            .Select(g => new ZeroResultQuery
            {
                Query = g.Key,
                Count = g.Count(),
                LastSeenOn = g.Max(q => q.SearchedOn),
            })
            .OrderByDescending(q => q.Count)
            .Take(20)
            .ToList();

        var byModule = queries
            .Where(q => !string.IsNullOrEmpty(q.ModuleFilter))
            .GroupBy(q => q.ModuleFilter!)
            .Select(g => new FacetBucket { Key = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count)
            .ToList();

        return new SearchAnalytics
        {
            TotalSearches = queries.Count,
            PopularQueries = popular,
            ZeroResultQueries = zeroResults,
            SearchesByModule = byModule,
        };
    }

    public async Task RecordSearchAsync(string query, int resultCount, CancellationToken cancellationToken = default)
    {
        var log = new SearchQueryLog(query, resultCount);
        _rptDb.SearchQueryLogs.Add(log);
        await _rptDb.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSearchEntryAsync(ReportDefinition report, CancellationToken cancellationToken)
    {
        var entry = await _rptDb.SearchIndexEntries
            .FirstOrDefaultAsync(e => e.SourceType == "ReportDefinition" && e.SourceId == report.Id, cancellationToken);

        if (entry == null)
        {
            entry = new SearchIndexEntry(report.Id, "ReportDefinition");
            _rptDb.SearchIndexEntries.Add(entry);
        }

        entry.Update(
            title: report.Name,
            description: report.Description,
            module: report.Module,
            category: report.Category,
            reportType: report.ReportType,
            searchText: BuildSearchText(report.Name, report.Description, report.Module, report.Category));
    }

    private async Task UpsertSearchEntryAsync(SavedQuery query, CancellationToken cancellationToken)
    {
        var entry = await _rptDb.SearchIndexEntries
            .FirstOrDefaultAsync(e => e.SourceType == "SavedQuery" && e.SourceId == query.Id, cancellationToken);

        if (entry == null)
        {
            entry = new SearchIndexEntry(query.Id, "SavedQuery");
            _rptDb.SearchIndexEntries.Add(entry);
        }

        entry.Update(
            title: query.Name,
            description: $"Saved query on {query.EntityName}",
            module: query.Module,
            category: query.QueryType,
            reportType: "SavedQuery",
            searchText: BuildSearchText(query.Name, query.EntityName, query.Module, query.QueryType));
    }

    private async Task UpsertSearchEntryAsync(FinancialStatementLayout layout, CancellationToken cancellationToken)
    {
        var entry = await _rptDb.SearchIndexEntries
            .FirstOrDefaultAsync(e => e.SourceType == "FinancialStatementLayout" && e.SourceId == layout.Id, cancellationToken);

        if (entry == null)
        {
            entry = new SearchIndexEntry(layout.Id, "FinancialStatementLayout");
            _rptDb.SearchIndexEntries.Add(entry);
        }

        entry.Update(
            title: layout.Name,
            description: layout.Description ?? $"{layout.StatementType} statement layout",
            module: "gl",
            category: "FinancialStatement",
            reportType: layout.StatementType,
            searchText: BuildSearchText(layout.Name, layout.Description, "gl", layout.StatementType));
    }

    private async Task UpsertSearchEntryAsync(QuickQuery qq, CancellationToken cancellationToken)
    {
        var entry = await _rptDb.SearchIndexEntries
            .FirstOrDefaultAsync(e => e.SourceType == "QuickQuery" && e.SourceId == qq.Id, cancellationToken);

        if (entry == null)
        {
            entry = new SearchIndexEntry(qq.Id, "QuickQuery");
            _rptDb.SearchIndexEntries.Add(entry);
        }

        entry.Update(
            title: qq.Name,
            description: $"Quick query on {qq.EntityName}",
            module: "platform",
            category: "QuickQuery",
            reportType: "QuickQuery",
            searchText: BuildSearchText(qq.Name, qq.EntityName, "platform", "QuickQuery"));
    }

    private static double ComputeRelevanceScore(SearchIndexEntry entry, string[] terms)
    {
        double score = 0;
        var searchText = entry.SearchText?.ToLowerInvariant() ?? string.Empty;
        var title = entry.Title?.ToLowerInvariant() ?? string.Empty;

        foreach (var term in terms)
        {
            // Exact title match: highest weight
            if (title.Contains(term, StringComparison.Ordinal))
            {
                score += 10.0;
            }

            // Search text match
            if (searchText.Contains(term, StringComparison.Ordinal))
            {
                score += 5.0;
            }

            // Partial match in title
            if (title.Split(' ').Any(w => w.StartsWith(term, StringComparison.Ordinal)))
            {
                score += 3.0;
            }
        }

        // Bonus for active items
        if (entry.IsActive)
        {
            score *= 1.1;
        }

        // Bonus for shared items
        if (entry.IsShared)
        {
            score *= 1.05;
        }

        return score;
    }

    private static string GenerateSnippet(SearchIndexEntry entry, string[] terms)
    {
        var text = entry.Description ?? entry.Title ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Find the first term occurrence and return a window around it
        var lowerText = text.ToLowerInvariant();
        foreach (var term in terms)
        {
            var idx = lowerText.IndexOf(term, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var start = Math.Max(0, idx - 40);
                var length = Math.Min(text.Length - start, 120);
                var snippet = text.Substring(start, length);
                var prefix = start > 0 ? "..." : string.Empty;
                var suffix = start + length < text.Length ? "..." : string.Empty;
                return string.Concat(prefix, snippet, suffix);
            }
        }

        // No term found; return first 120 chars
        return text.Length > 120 ? string.Concat(text.AsSpan(0, 120), "...") : text;
    }

    private static string BuildSearchText(string name, string? description, string module, string category)
    {
        return string.IsNullOrEmpty(description)
            ? $"{name} {module} {category}"
            : $"{name} {description} {module} {category}";
    }
}

/// <summary>
/// Tracks when the search index was last synced and how many records were indexed.
/// </summary>
public class SearchIndexSyncState
{
    protected SearchIndexSyncState() { }

    public SearchIndexSyncState(string id)
    {
        StringId = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Gets the string-based primary key for sync state entries (e.g., "last-sync").
    /// </summary>
    public string StringId { get; private set; } = string.Empty;
    public DateTimeOffset? LastSyncOn { get; private set; }
    public long RecordsIndexed { get; private set; }

    public void RecordSync(int count)
    {
        LastSyncOn = DateTimeOffset.UtcNow;
        RecordsIndexed = count;
    }
}

/// <summary>
/// Logs each search query for analytics: popular queries, zero-result queries,
/// and module usage patterns.
/// </summary>
public class SearchQueryLog : Entity
{
    protected SearchQueryLog() { }

    public SearchQueryLog(string query, int resultCount)
    {
        Id = Guid.NewGuid();
        Query = query ?? string.Empty;
        ResultCount = resultCount;
        SearchedOn = DateTimeOffset.UtcNow;
    }

    public string Query { get; private set; } = string.Empty;
    public int ResultCount { get; private set; }
    public string? ModuleFilter { get; private set; }
    public string? UserIdentity { get; private set; }
    public DateTimeOffset SearchedOn { get; private set; }

    public void SetContext(string? moduleFilter, string? userIdentity)
    {
        ModuleFilter = moduleFilter;
        UserIdentity = userIdentity;
    }
}

/// <summary>
/// Represents a single entry in the search index. Each report definition,
/// saved query, financial statement layout, and quick query has one entry.
/// </summary>
public class SearchIndexEntry : Entity
{
    protected SearchIndexEntry() { }

    public SearchIndexEntry(Guid sourceId, string sourceType)
    {
        Id = Guid.NewGuid();
        SourceId = sourceId;
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        IsActive = true;
        IsShared = false;
        IndexedOn = DateTimeOffset.UtcNow;
    }

    public Guid SourceId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Module { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string ReportType { get; private set; } = string.Empty;
    public string? SearchText { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsShared { get; private set; }
    public DateTimeOffset IndexedOn { get; private set; }

    public void Update(
        string title,
        string? description,
        string module,
        string category,
        string reportType,
        string searchText)
    {
        Title = title ?? string.Empty;
        Description = description;
        Module = module ?? string.Empty;
        Category = category ?? string.Empty;
        ReportType = reportType ?? string.Empty;
        SearchText = searchText ?? string.Empty;
        IndexedOn = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
    public void SetShared(bool isShared) => IsShared = isShared;
}
