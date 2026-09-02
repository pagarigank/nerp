// <copyright file="WebhookSubscriptionController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Integration.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/webhooks")]
public class WebhookSubscriptionController : ControllerBase
{
    private readonly IntegrationDbContext _context;

    public WebhookSubscriptionController(IntegrationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WebhookDto>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] string? eventType, CancellationToken ct)
    {
        var query = _context.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(x => x.EventType == eventType);

        var webhooks = await query.OrderBy(x => x.EventType).ToListAsync(ct);
        return Ok(webhooks.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WebhookDto>> GetById(Guid id, CancellationToken ct)
    {
        var wh = await _context.WebhookSubscriptions.FindAsync(new object[] { id }, ct);
        return wh is null ? NotFound() : Ok(MapToDto(wh));
    }

    [HttpPost]
    public async Task<ActionResult<WebhookDto>> Create(
        [FromBody] CreateWebhookRequest request, CancellationToken ct)
    {
        var wh = new WebhookSubscription(
            request.CompanyId, request.EventType, request.TargetUrl,
            request.Secret, request.PayloadFormat, request.Description,
            request.MaxRetries, request.RetryDelaySeconds);
        _context.WebhookSubscriptions.Add(wh);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = wh.Id }, MapToDto(wh));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WebhookDto>> Update(
        Guid id, [FromBody] UpdateWebhookRequest request, CancellationToken ct)
    {
        var wh = await _context.WebhookSubscriptions.FindAsync(new object[] { id }, ct);
        if (wh is null) return NotFound();
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) wh.Activate(); else wh.Deactivate();
        }

        await _context.SaveChangesAsync(ct);
        return Ok(MapToDto(wh));
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id, CancellationToken ct)
    {
        var wh = await _context.WebhookSubscriptions.FindAsync(new object[] { id }, ct);
        if (wh is null) return NotFound();
        var log = new WebhookDeliveryLog(id, "test.ping", wh.TargetUrl, "{\"test\":true}", 1);
        _context.WebhookDeliveryLogs.Add(log);
        await _context.SaveChangesAsync(ct);
        return Ok(new { logId = log.Id, message = "Test webhook queued for delivery." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var wh = await _context.WebhookSubscriptions.FindAsync(new object[] { id }, ct);
        if (wh is null) return NotFound();
        _context.WebhookSubscriptions.Remove(wh);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("event-types")]
    public ActionResult<IReadOnlyList<string>> GetEventTypes()
    {
        return Ok(new[]
        {
            "VoucherPosted", "PaymentIssued", "InvoicePosted", "CashApplied",
            "ShipmentConfirmed", "POApproved", "PayrollPosted", "BatchPosted",
            "PeriodLocked", "GoodsReceived", "ItemIssued", "WorkOrderCompleted"
        });
    }

    [HttpGet("delivery-logs")]
    public async Task<ActionResult<IReadOnlyList<WebhookDeliveryLogDto>>> GetDeliveryLogs(
        [FromQuery] Guid? webhookSubscriptionId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var query = _context.WebhookDeliveryLogs
            .AsNoTracking()
            .Where(x => !webhookSubscriptionId.HasValue || x.WebhookSubscriptionId == webhookSubscriptionId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var logs = await query
            .OrderByDescending(x => x.DeliveredOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return Ok(logs.Select(MapToLogDto).ToList());
    }

    private static WebhookDto MapToDto(WebhookSubscription w) => new(
        w.Id, w.CompanyId, w.EventType, w.TargetUrl, w.PayloadFormat,
        w.Description, w.IsActive, w.MaxRetries, w.RetryDelaySeconds,
        w.LastDeliveredOn, w.LastDeliveryStatus);

    private static WebhookDeliveryLogDto MapToLogDto(WebhookDeliveryLog l) => new(
        l.Id, l.WebhookSubscriptionId, l.EventType, l.TargetUrl,
        l.AttemptNumber, l.Status, l.HttpStatusCode, l.DurationMs,
        l.ErrorMessage, l.DeliveredOn);

    public record WebhookDto(
        Guid Id, Guid CompanyId, string EventType, string TargetUrl,
        string PayloadFormat, string? Description, bool IsActive,
        int MaxRetries, int RetryDelaySeconds,
        DateTimeOffset? LastDeliveredOn, int? LastDeliveryStatus);

    public record WebhookDeliveryLogDto(
        Guid Id, Guid WebhookSubscriptionId, string EventType, string TargetUrl,
        int AttemptNumber, string Status, int? HttpStatusCode, long? DurationMs,
        string? ErrorMessage, DateTimeOffset DeliveredOn);

    public record CreateWebhookRequest(
        Guid CompanyId, string EventType, string TargetUrl,
        string? Secret, string PayloadFormat, string? Description,
        int MaxRetries, int RetryDelaySeconds);

    public record UpdateWebhookRequest(bool? IsActive);
}
