// <copyright file="BankStatementDownloadJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.CashManagement.Infrastructure;

public record StatementFeedConfig(
    Guid BankAccountId,
    string Endpoint,
    string? Format);

public record BankStatementDownloadReport(
    int FeedsProcessed,
    int Imported,
    int SkippedExisting,
    IReadOnlyList<string> Errors);

public interface IBankStatementDownloadJob
{
    Task<BankStatementDownloadReport> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Automated bank statement download (Cash Management Phase 5 background job). Pulls
/// statements from per-bank-account HTTP feeds configured under
/// <c>CashManagement:StatementFeeds</c> (OFX/QBO/CSV/BAI2 — format is
/// auto-detected when not specified), reusing the same parser and duplicate
/// validation as manual import. Statements whose number already exists for the
/// account are skipped, so the job is safe on every schedule tick.
/// </summary>
public class BankStatementDownloadJob : IBankStatementDownloadJob
{
    private readonly CashDbContext _context;
    private readonly IBankStatementParserService _parser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BankStatementDownloadJob> _logger;

    public BankStatementDownloadJob(
        CashDbContext context,
        IBankStatementParserService parser,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BankStatementDownloadJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BankStatementDownloadReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var feeds = LoadFeeds();
        var errors = new List<string>();
        var imported = 0;
        var skipped = 0;

        if (feeds.Count == 0)
        {
            _logger.LogDebug("No CashManagement:StatementFeeds configured; statement download skipped.");
            return new BankStatementDownloadReport(0, 0, 0, errors);
        }

        var client = _httpClientFactory.CreateClient("bank-feeds");

        foreach (var feed in feeds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var accountExists = await _context.BankAccounts
                    .AnyAsync(a => a.Id == feed.BankAccountId && !a.DeletedOn.HasValue, cancellationToken);
                if (!accountExists)
                {
                    skipped++;
                    continue;
                }

                using var response = await client.GetAsync(new Uri(feed.Endpoint), cancellationToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                BankStatementFormat? expectedFormat = null;
                if (!string.IsNullOrWhiteSpace(feed.Format)
                    && Enum.TryParse<BankStatementFormat>(feed.Format, ignoreCase: true, out var fmt))
                {
                    expectedFormat = fmt;
                }

                var parsed = _parser.Parse(content, expectedFormat);

                // Content-derived number: identical re-downloads dedupe naturally.
                var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
                var statementNumber = $"DL-{DateTimeOffset.UtcNow:yyyyMMdd}-{contentHash[..12]}";

                var exists = await _context.BankStatements.AnyAsync(
                    s => s.BankAccountId == feed.BankAccountId
                        && s.StatementNumber == statementNumber
                        && !s.DeletedOn.HasValue,
                    cancellationToken);

                if (exists)
                {
                    skipped++;
                    continue;
                }

                var account = await _context.BankAccounts.FirstAsync(a => a.Id == feed.BankAccountId, cancellationToken);
                var statement = new BankStatement(
                    account.CompanyId,
                    feed.BankAccountId,
                    statementNumber,
                    parsed.AsOfDate ?? DateTimeOffset.UtcNow,
                    parsed.BeginningBalance ?? 0,
                    parsed.EndingBalance ?? 0,
                    $"download-{statementNumber}.{parsed.Format.ToString().ToUpperInvariant()}",
                    parsed.Format);

                foreach (var line in parsed.Lines)
                {
                    statement.AddLine(
                        line.TransactionDate,
                        line.Amount,
                        line.Description,
                        line.ReferenceNumber,
                        line.CheckNumber,
                        line.Balance ?? 0);
                }

                if (parsed.Lines.Count > 0)
                {
                    statement.MarkValidated();
                }

                _context.BankStatements.Add(statement);
                await _context.SaveChangesAsync(cancellationToken);
                imported++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Bank statement download failed for feed {Endpoint}.", feed.Endpoint);
                errors.Add($"{feed.Endpoint}: {ex.Message}");
            }
        }

        return new BankStatementDownloadReport(feeds.Count, imported, skipped, errors);
    }

    private List<StatementFeedConfig> LoadFeeds()
    {
        var section = _configuration.GetSection("CashManagement:StatementFeeds");
        if (!section.Exists())
        {
            return [];
        }

        return section.GetChildren()
            .Select(c =>
            {
                var bankAccountId = c.GetValue<Guid?>("BankAccountId");
                var url = c.GetValue<string>("Url");
                if (bankAccountId is null || string.IsNullOrWhiteSpace(url))
                {
                    return null;
                }

                return new StatementFeedConfig(bankAccountId.Value, url!, c.GetValue<string?>("Format"));
            })
            .Where(f => f is not null)
            .Select(f => f!)
            .ToList();
    }
}
