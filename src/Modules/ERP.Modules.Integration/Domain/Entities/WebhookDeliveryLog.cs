// <copyright file="WebhookDeliveryLog.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class WebhookDeliveryLog : AuditableEntity
{
    protected WebhookDeliveryLog() { }

    public WebhookDeliveryLog(
        Guid webhookSubscriptionId,
        string eventType,
        string targetUrl,
        string payload,
        int attemptNumber)
    {
        Id = Guid.NewGuid();
        WebhookSubscriptionId = webhookSubscriptionId;
        EventType = eventType;
        TargetUrl = targetUrl;
        Payload = payload;
        AttemptNumber = attemptNumber;
        Status = "Pending";
        DeliveredOn = DateTimeOffset.UtcNow;
    }

    public Guid WebhookSubscriptionId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public string Payload { get; private set; } = "{}";
    public int AttemptNumber { get; internal set; }
    public string Status { get; private set; } = "Pending";
    public int? HttpStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset DeliveredOn { get; private set; }
    public long? DurationMs { get; private set; }

    public void MarkSuccess(int httpStatus, string? responseBody, long durationMs)
    {
        Status = "Success";
        HttpStatusCode = httpStatus;
        ResponseBody = responseBody?.Length > 4000 ? responseBody[..4000] : responseBody;
        DurationMs = durationMs;
    }

    public void MarkFailed(int httpStatus, string? errorMessage, long durationMs)
    {
        Status = "Failed";
        HttpStatusCode = httpStatus;
        ErrorMessage = errorMessage?.Length > 2000 ? errorMessage[..2000] : errorMessage;
        DurationMs = durationMs;
    }

    public void MarkPendingRetry()
    {
        Status = "PendingRetry";
    }
}
