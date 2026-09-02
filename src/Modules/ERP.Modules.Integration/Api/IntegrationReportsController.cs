// <copyright file="IntegrationReportsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Integration.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/reports")]
#pragma warning disable S6960
public class IntegrationReportsController : ControllerBase
#pragma warning restore S6960
{
    private readonly IntegrationDbContext _context;

    public IntegrationReportsController(IntegrationDbContext context) => _context = context;

    [HttpGet("edi-transaction-log")]
    public async Task<ActionResult<IReadOnlyList<EdiTransactionLogDto>>> GetEdiTransactionLog(
        [FromQuery] Guid? companyId, [FromQuery] string? documentType,
        [FromQuery] string? direction, [FromQuery] string? status,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var query = _context.EdiTransactionLogs
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(documentType))
            query = query.Where(x => x.DocumentType == documentType);
        if (!string.IsNullOrEmpty(direction))
            query = query.Where(x => x.Direction == direction);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);
        if (from.HasValue)
            query = query.Where(x => x.ProcessedOn >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.ProcessedOn <= to.Value);

        var logs = await query
            .OrderByDescending(x => x.ProcessedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(logs.Select(l => new EdiTransactionLogDto(
            l.Id, l.PartnerCode, l.DocumentType, l.Direction,
            l.IsaControlNumber, l.GsControlNumber, l.StControlNumber,
            l.Status, l.ErrorMessage, l.ProcessedOn, l.AcknowledgedOn)).ToList());
    }

    [HttpGet("integration-health")]
    public async Task<ActionResult<IntegrationHealthDto>> GetHealthDashboard(
        [FromQuery] Guid? companyId, CancellationToken ct)
    {
        var partnerCount = await _context.TradingPartners
            .CountAsync(x => !companyId.HasValue || x.CompanyId == companyId.Value, ct);
        var activePartners = await _context.TradingPartners
            .CountAsync(x => (!companyId.HasValue || x.CompanyId == companyId.Value) && x.IsActive, ct);
        var webhookCount = await _context.WebhookSubscriptions
            .CountAsync(x => !companyId.HasValue || x.CompanyId == companyId.Value, ct);
        var activeWebhooks = await _context.WebhookSubscriptions
            .CountAsync(x => (!companyId.HasValue || x.CompanyId == companyId.Value) && x.IsActive, ct);

        var last24h = DateTimeOffset.UtcNow.AddHours(-24);
        var ediProcessed24h = await _context.EdiTransactionLogs
            .CountAsync(x => x.ProcessedOn >= last24h, ct);
        var ediFailed24h = await _context.EdiTransactionLogs
            .CountAsync(x => x.ProcessedOn >= last24h && x.Status == "Failed", ct);
        var webhookDeliveries24h = await _context.WebhookDeliveryLogs
            .CountAsync(x => x.DeliveredOn >= last24h, ct);
        var webhookFailed24h = await _context.WebhookDeliveryLogs
            .CountAsync(x => x.DeliveredOn >= last24h && x.Status == "Failed", ct);

        var pendingAcks = await _context.EdiAcks
            .CountAsync(x => x.Status == "Received", ct);
        var expiredCerts = await _context.TradingPartners
            .CountAsync(x => x.CertificateExpiresOn.HasValue && x.CertificateExpiresOn.Value < DateTimeOffset.UtcNow, ct);

        return Ok(new IntegrationHealthDto(
            partnerCount, activePartners, webhookCount, activeWebhooks,
            ediProcessed24h, ediFailed24h, webhookDeliveries24h, webhookFailed24h,
            pendingAcks, expiredCerts));
    }

    [HttpGet("trading-partner-performance")]
    public async Task<ActionResult<IReadOnlyList<TradingPartnerPerformanceDto>>> GetPartnerPerformance(
        [FromQuery] Guid? companyId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var partners = await _context.TradingPartners
            .AsNoTracking()
            .Where(x => (!companyId.HasValue || x.CompanyId == companyId.Value) && x.IsActive)
            .ToListAsync(ct);

        var result = new List<TradingPartnerPerformanceDto>();
        foreach (var partner in partners)
        {
            var totalTx = await _context.EdiTransactionLogs
                .CountAsync(x => x.TradingPartnerId == partner.Id && x.ProcessedOn >= since, ct);
            var failedTx = await _context.EdiTransactionLogs
                .CountAsync(x => x.TradingPartnerId == partner.Id && x.ProcessedOn >= since && x.Status == "Failed", ct);
            var ackedTx = await _context.EdiTransactionLogs
                .CountAsync(x => x.TradingPartnerId == partner.Id && x.ProcessedOn >= since && x.Status == "Acknowledged", ct);
            var errorRate = totalTx > 0 ? (decimal)failedTx / totalTx * 100 : 0;
            var ackRate = totalTx > 0 ? (decimal)ackedTx / totalTx * 100 : 0;

            result.Add(new TradingPartnerPerformanceDto(
                partner.Id, partner.PartnerCode, partner.Name,
                totalTx, failedTx, ackedTx,
                Math.Round(errorRate, 1), Math.Round(ackRate, 1), days));
        }

        return Ok(result.OrderByDescending(x => x.TotalTransactions).ToList());
    }

    public record EdiTransactionLogDto(
        Guid Id, string PartnerCode, string DocumentType, string Direction,
        string? IsaControlNumber, string? GsControlNumber, string? StControlNumber,
        string Status, string? ErrorMessage, DateTimeOffset ProcessedOn,
        DateTimeOffset? AcknowledgedOn);

    public record IntegrationHealthDto(
        int TotalTradingPartners, int ActiveTradingPartners,
        int TotalWebhooks, int ActiveWebhooks,
        int EdiProcessed24h, int EdiFailed24h,
        int WebhookDeliveries24h, int WebhookFailed24h,
        int PendingAcks, int ExpiredCertificates);

    public record TradingPartnerPerformanceDto(
        Guid PartnerId, string PartnerCode, string PartnerName,
        int TotalTransactions, int FailedTransactions, int AcknowledgedTransactions,
        decimal ErrorRatePercent, decimal AcknowledgmentRatePercent, int PeriodDays);
}
