// <copyright file="ShipmentConfirmedToEdiHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.OrderManagement.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Integration.Infrastructure.Handlers;

/// <summary>
/// Handles ShipmentConfirmedEvent and generates outbound EDI 856 (ASN)
/// documents for trading partners that have EDI document mappings configured.
/// </summary>
public sealed class ShipmentConfirmedToEdiHandler : IDomainEventHandler<ShipmentConfirmedEvent>
{
    private readonly OutboundEdiGenerator _generator;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<ShipmentConfirmedToEdiHandler> _logger;

    public ShipmentConfirmedToEdiHandler(
        OutboundEdiGenerator generator,
        IntegrationDbContext dbContext,
        ILogger<ShipmentConfirmedToEdiHandler> logger)
    {
        _generator = generator;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(ShipmentConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ShipmentConfirmed: ShipmentId={ShipmentId}, ShipmentNumber={ShipmentNumber}, CompanyId={CompanyId}",
            domainEvent.ShipmentId, domainEvent.ShipmentNumber, domainEvent.CompanyId);

        try
        {
            // Check if any trading partner has an EDI 856 mapping for this company
            var hasEdiMapping = await _dbContext.EdiDocumentMappings
                .AnyAsync(m => m.CompanyId == domainEvent.CompanyId
                    && m.DocumentType == "856"
                    && m.Direction == "Outbound"
                    && m.IsActive,
                    cancellationToken);

            if (!hasEdiMapping)
            {
                _logger.LogDebug(
                    "No EDI 856 mapping configured for company {CompanyId}. Skipping EDI generation.",
                    domainEvent.CompanyId);
                return;
            }

            // Find the trading partner for this shipment's customer
            var partner = await _dbContext.TradingPartners
                .Where(p => p.CompanyId == domainEvent.CompanyId && p.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            var result = await _generator.GenerateShipment856Async(
                companyId: domainEvent.CompanyId,
                shipmentId: domainEvent.ShipmentId,
                shipmentNumber: domainEvent.ShipmentNumber,
                shipmentDate: domainEvent.ShipmentDate,
                customerId: domainEvent.CustomerId,
                carrier: domainEvent.Carrier,
                trackingNumber: domainEvent.TrackingNumber,
                partnerCode: partner?.PartnerCode,
                ct: cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "EDI 856 generated for shipment {ShipmentNumber}: {Length} chars",
                    domainEvent.ShipmentNumber, result.EdiContent?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning(
                    "EDI 856 generation failed for shipment {ShipmentNumber}: {Error}",
                    domainEvent.ShipmentNumber, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating EDI 856 for shipment {ShipmentNumber}",
                domainEvent.ShipmentNumber);
        }
    }
}
