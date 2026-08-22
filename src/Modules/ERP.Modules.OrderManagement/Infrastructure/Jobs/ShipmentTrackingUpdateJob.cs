// <copyright file="ShipmentTrackingUpdateJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Net.Http;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.OrderManagement.Infrastructure.Jobs;

public interface IShipmentTrackingUpdateJob
{
    Task<ShipmentTrackingReport> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record ShipmentTrackingReport(int ShipmentsChecked, int MarkedDelivered, int Failures);

/// <summary>
/// Polls the configured carrier tracking endpoint for shipments that carry a
/// tracking number but no delivery confirmation yet. The carrier endpoint is
/// built from <c>OrderManagement:CarrierTracking:BaseUrl</c> with a
/// <c>{trackingNumber}</c> placeholder and called through the named
/// <c>carrier-tracking</c> HTTP client; when the response contains the
/// configured <c>DeliveredMarker</c> substring the shipment is marked delivered.
/// Per-shipment failures are logged and counted — never thrown — so one bad
/// tracking number cannot abort the sweep.
/// </summary>
public class ShipmentTrackingUpdateJob : IShipmentTrackingUpdateJob
{
    private const string DefaultDeliveredMarker = "delivered";

    private readonly OmDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShipmentTrackingUpdateJob> _logger;

    public ShipmentTrackingUpdateJob(
        OmDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ShipmentTrackingUpdateJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ShipmentTrackingReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["OrderManagement:CarrierTracking:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogDebug("OrderManagement:CarrierTracking:BaseUrl is not configured; shipment tracking update skipped.");
            return new ShipmentTrackingReport(0, 0, 0);
        }

        var deliveredMarker = _configuration["OrderManagement:CarrierTracking:DeliveredMarker"];
        if (string.IsNullOrWhiteSpace(deliveredMarker))
        {
            deliveredMarker = DefaultDeliveredMarker;
        }

        var shipments = await _context.Shipments
            .Where(s => s.TrackingNumber != null && s.DeliveredOn == null)
            .ToListAsync(cancellationToken);

        var checkedCount = 0;
        var delivered = 0;
        var failures = 0;
        var client = _httpClientFactory.CreateClient("carrier-tracking");

        foreach (var shipment in shipments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                checkedCount++;
                var trackingNumber = shipment.TrackingNumber!;
                var endpoint = baseUrl.Replace("{trackingNumber}", Uri.EscapeDataString(trackingNumber), StringComparison.OrdinalIgnoreCase);
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var requestUri))
                {
                    failures++;
                    _logger.LogWarning(
                        "Invalid carrier tracking endpoint '{Endpoint}' for shipment {ShipmentNumber}; check OrderManagement:CarrierTracking:BaseUrl.",
                        endpoint,
                        shipment.ShipmentNumber);
                    continue;
                }

                using var response = await client.GetAsync(requestUri, cancellationToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (content.Contains(deliveredMarker, StringComparison.OrdinalIgnoreCase))
                {
                    shipment.MarkDelivered();
                    delivered++;
                    _logger.LogInformation(
                        "Shipment {ShipmentNumber} (tracking {TrackingNumber}) confirmed delivered by carrier feed.",
                        shipment.ShipmentNumber,
                        trackingNumber);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures++;
                _logger.LogWarning(
                    ex,
                    "Carrier tracking check failed for shipment {ShipmentNumber} (tracking {TrackingNumber}).",
                    shipment.ShipmentNumber,
                    shipment.TrackingNumber);
            }
        }

        if (delivered > 0)
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Shipment tracking update completed: {Checked} shipment(s) polled, {Delivered} marked delivered, {Failures} failure(s).",
            checkedCount,
            delivered,
            failures);

        return new ShipmentTrackingReport(checkedCount, delivered, failures);
    }
}
