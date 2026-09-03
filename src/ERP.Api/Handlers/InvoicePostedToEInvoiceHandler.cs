// <copyright file="InvoicePostedToEInvoiceHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Api.Handlers;

/// <summary>
/// Handles InvoiceBatchPostedEvent and auto-submits e-Invoices for each invoice in the batch.
/// This is the ERP integration hook that connects the AR posting pipeline to the MyInvois submission pipeline.
/// </summary>
public sealed class InvoicePostedToEInvoiceHandler : IDomainEventHandler<InvoiceBatchPostedEvent>
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IEInvoiceService _eInvoiceService;
    private readonly ILogger<InvoicePostedToEInvoiceHandler> _logger;

    public InvoicePostedToEInvoiceHandler(
        IEInvoiceService eInvoiceService,
        ILogger<InvoicePostedToEInvoiceHandler> logger)
    {
        _eInvoiceService = eInvoiceService;
        _logger = logger;
    }

    public async Task HandleAsync(InvoiceBatchPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "InvoiceBatchPosted event received: BatchId={BatchId}, BatchNumber={BatchNumber}, CompanyId={CompanyId}",
            domainEvent.BatchId, domainEvent.BatchNumber, domainEvent.CompanyId);

        // Check if the company has e-Invoice enabled
        // For now, we attempt submission for all companies. In production,
        // this would check a company setting or TaxpayerProfile existence.
        try
        {
            // Build a simple UBL payload for each invoice in the batch
            // In production, this would load the full invoice data from the AR module
            var payload = BuildInvoicePayload(domainEvent);

            var result = await _eInvoiceService.SubmitDocumentAsync(
                companyId: domainEvent.CompanyId,
                sourceDocumentId: domainEvent.BatchId,
                documentType: "Invoice",
                payload: payload,
                cancellationToken: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "E-Invoice submitted for batch {BatchNumber}: UUID={Uuid}, DocumentId={DocumentId}",
                    domainEvent.BatchNumber, result.Uuid, result.DocumentId);
            }
            else
            {
                _logger.LogWarning(
                    "E-Invoice submission failed for batch {BatchNumber}: Error={Error}",
                    domainEvent.BatchNumber, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Don't let e-Invoice failure block the invoice posting
            _logger.LogError(ex,
                "Error submitting e-Invoice for batch {BatchNumber}. The invoice was posted successfully but e-Invoice submission failed.",
                domainEvent.BatchNumber);
        }
    }

    private static string BuildInvoicePayload(InvoiceBatchPostedEvent batchEvent)
    {
        // Build a minimal UBL JSON payload from the batch event data.
        // In production, this would load the full invoice lines from the database
        // and populate all 37+ mandatory fields using UblGenerator.
        var dto = new ERP.Modules.EInvoice.Domain.UblInvoiceDto
        {
            InvoiceTypeCode = "01",
            InvoiceNumber = batchEvent.BatchNumber,
            IssueDate = batchEvent.PostingDate.Date,
            IssueTime = batchEvent.PostingDate.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z",
            CurrencyCode = "MYR",
            ExchangeRate = 1.0m,
            Supplier = new ERP.Modules.EInvoice.Domain.UblPartyDto
            {
                Name = "ERP Company",
                TIN = "C230601D123456",
                RegistrationNumber = "202301001234",
            },
            Buyer = new ERP.Modules.EInvoice.Domain.UblPartyDto
            {
                Name = "Customer",
                TIN = "N/A",
                RegistrationNumber = "N/A",
            },
        };

        return System.Text.Json.JsonSerializer.Serialize(
            new { invoice = dto },
            JsonOptions);
    }
}
