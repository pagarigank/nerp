// <copyright file="InvoicePostedToEdiHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Integration.Infrastructure.Handlers;

/// <summary>
/// Handles InvoiceBatchPostedEvent and generates outbound EDI 810 (Invoice)
/// documents for trading partners that have EDI document mappings configured.
/// </summary>
public sealed class InvoicePostedToEdiHandler : IDomainEventHandler<InvoiceBatchPostedEvent>
{
    private readonly OutboundEdiGenerator _generator;
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<InvoicePostedToEdiHandler> _logger;

    public InvoicePostedToEdiHandler(
        OutboundEdiGenerator generator,
        IntegrationDbContext dbContext,
        ILogger<InvoicePostedToEdiHandler> logger)
    {
        _generator = generator;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(InvoiceBatchPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "InvoiceBatchPosted: BatchId={BatchId}, BatchNumber={BatchNumber}, CompanyId={CompanyId}",
            domainEvent.BatchId, domainEvent.BatchNumber, domainEvent.CompanyId);

        try
        {
            // Check if any trading partner has an EDI 810 mapping for this company
            var hasEdiMapping = await _dbContext.EdiDocumentMappings
                .AnyAsync(m => m.CompanyId == domainEvent.CompanyId
                    && m.DocumentType == "810"
                    && m.Direction == "Outbound"
                    && m.IsActive,
                    cancellationToken);

            if (!hasEdiMapping)
            {
                _logger.LogDebug(
                    "No EDI 810 mapping configured for company {CompanyId}. Skipping EDI generation.",
                    domainEvent.CompanyId);
                return;
            }

            // Find the trading partner for this batch
            // In a real implementation, the batch would reference a customer
            // which maps to a trading partner. For now, use the first active partner.
            var partner = await _dbContext.TradingPartners
                .Where(p => p.CompanyId == domainEvent.CompanyId && p.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            var result = await _generator.GenerateInvoice810Async(
                companyId: domainEvent.CompanyId,
                batchId: domainEvent.BatchId,
                batchNumber: domainEvent.BatchNumber,
                postingDate: domainEvent.PostingDate,
                currencyCode: domainEvent.CurrencyCode,
                partnerCode: partner?.PartnerCode,
                ct: cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "EDI 810 generated for batch {BatchNumber}: {Length} chars",
                    domainEvent.BatchNumber, result.EdiContent?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning(
                    "EDI 810 generation failed for batch {BatchNumber}: {Error}",
                    domainEvent.BatchNumber, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating EDI 810 for batch {BatchNumber}",
                domainEvent.BatchNumber);
        }
    }
}
