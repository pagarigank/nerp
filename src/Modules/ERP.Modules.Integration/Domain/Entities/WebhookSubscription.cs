// <copyright file="WebhookSubscription.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class WebhookSubscription : AuditableEntity
{
    protected WebhookSubscription() { }

    public WebhookSubscription(
        Guid companyId,
        string eventType,
        string targetUrl,
        string? secret,
        string payloadFormat,
        string? description,
        int maxRetries,
        int retryDelaySeconds)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        EventType = eventType;
        TargetUrl = targetUrl;
        Secret = secret;
        PayloadFormat = payloadFormat;
        Description = description;
        MaxRetries = maxRetries;
        RetryDelaySeconds = retryDelaySeconds;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public string? Secret { get; private set; }
    public string PayloadFormat { get; private set; } = "json";
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int MaxRetries { get; private set; } = 3;
    public int RetryDelaySeconds { get; private set; } = 30;
    public DateTimeOffset? LastDeliveredOn { get; private set; }
    public int? LastDeliveryStatus { get; private set; }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void RecordDelivery(int httpStatus)
    {
        LastDeliveredOn = DateTimeOffset.UtcNow;
        LastDeliveryStatus = httpStatus;
    }
}
