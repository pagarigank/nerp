// <copyright file="InvoicePostedToEInvoiceHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.EInvoice.Infrastructure;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Api.Handlers;

/// <summary>
/// Handles InvoiceBatchPostedEvent and auto-submits e-Invoices.
/// Loads supplier/buyer profiles, item extensions, and tax determination rules.
/// </summary>
public sealed class InvoicePostedToEInvoiceHandler : IDomainEventHandler<InvoiceBatchPostedEvent>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IEInvoiceService _eInvoiceService;
    private readonly EInvoiceDbContext _eInvoiceDbContext;
    private readonly ERP.Modules.MalaysianTax.Infrastructure.MalaysianTaxDbContext _taxDbContext;
    private readonly ILogger<InvoicePostedToEInvoiceHandler> _logger;

    public InvoicePostedToEInvoiceHandler(
        IEInvoiceService eInvoiceService,
        EInvoiceDbContext eInvoiceDbContext,
        ERP.Modules.MalaysianTax.Infrastructure.MalaysianTaxDbContext taxDbContext,
        ILogger<InvoicePostedToEInvoiceHandler> logger)
    {
        _eInvoiceService = eInvoiceService;
        _eInvoiceDbContext = eInvoiceDbContext;
        _taxDbContext = taxDbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(InvoiceBatchPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "InvoiceBatchPosted: BatchId={BatchId}, BatchNumber={BatchNumber}, CompanyId={CompanyId}",
            domainEvent.BatchId, domainEvent.BatchNumber, domainEvent.CompanyId);

        try
        {
            var payload = await BuildInvoicePayloadAsync(domainEvent, cancellationToken);

            var result = await _eInvoiceService.SubmitDocumentAsync(
                companyId: domainEvent.CompanyId,
                sourceDocumentId: domainEvent.BatchId,
                documentType: "Invoice",
                payload: payload,
                cancellationToken: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "E-Invoice submitted for batch {BatchNumber}: UUID={Uuid}",
                    domainEvent.BatchNumber, result.Uuid);
            }
            else
            {
                _logger.LogWarning(
                    "E-Invoice submission failed for batch {BatchNumber}: {Error}",
                    domainEvent.BatchNumber, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error submitting e-Invoice for batch {BatchNumber}.",
                domainEvent.BatchNumber);
        }
    }

    private static UblPartyDto BuildSupplierParty(TaxpayerProfile? profile)
    {
        if (profile == null)
        {
            return new UblPartyDto
            {
                Name = "UNKNOWN SUPPLIER",
                TIN = "TBC",
                RegistrationNumber = "TBC",
            };
        }

        return new UblPartyDto
        {
            Name = profile.BusinessRegistrationNumber ?? "ERP Company",
            TIN = profile.TaxIdentificationNumber,
            RegistrationNumber = profile.BusinessRegistrationNumber ?? string.Empty,
            IdType = "BRN",
            SSTRegistrationNumber = profile.SalesTaxNumber ?? profile.ServiceTaxNumber,
            MsicCode = profile.MsicCode,
            BusinessActivityDescription = profile.MsicCode,
            Address = new UblAddressDto
            {
                AddressLines = new List<string> { profile.Address ?? string.Empty },
                City = profile.City ?? string.Empty,
                State = profile.State ?? string.Empty,
                PostalCode = profile.PostCode ?? string.Empty,
                CountryCode = "MYS",
            },
        };
    }

    private static string DetermineTaxCode(List<TaxDeterminationRule> rules)
    {
        return rules.Count > 0 ? rules[0].TaxCodeId.ToString() : "01";
    }

    private static List<UblTaxSubtotalDto> ComputeTaxSubtotals(List<UblInvoiceLineDto> lineItems)
    {
        var subtotals = new List<UblTaxSubtotalDto>();

        var grouped = lineItems.GroupBy(l => l.TaxTypeCode).ToList();

        foreach (var group in grouped)
        {
            subtotals.Add(new UblTaxSubtotalDto
            {
                TaxableAmount = group.Sum(l => l.LineExtensionAmount),
                TaxAmount = group.Sum(l => l.TaxAmount),
                TaxTypeCode = group.Key,
                TaxRatePercent = group.FirstOrDefault()?.TaxRatePercent ?? 0,
            });
        }

        return subtotals;
    }

    private async Task<string> BuildInvoicePayloadAsync(InvoiceBatchPostedEvent batchEvent, CancellationToken cancellationToken)
    {
        var supplierProfile = await _eInvoiceDbContext.TaxpayerProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == batchEvent.CompanyId, cancellationToken);

        var supplier = BuildSupplierParty(supplierProfile);
        var buyer = await BuildBuyerPartyAsync(batchEvent.CompanyId, cancellationToken);
        var lineItems = await BuildLineItemsAsync(batchEvent, cancellationToken);
        var taxSubtotals = ComputeTaxSubtotals(lineItems);

        var totalExcl = lineItems.Sum(l => l.LineExtensionAmount);
        var totalTax = taxSubtotals.Sum(t => t.TaxAmount);

        var dto = new UblInvoiceDto
        {
            InvoiceTypeCode = "01",
            InvoiceNumber = batchEvent.BatchNumber,
            IssueDate = batchEvent.PostingDate.Date,
            IssueTime = batchEvent.PostingDate.ToString(
                "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z",
            CurrencyCode = "MYR",
            ExchangeRate = 1.0m,
            Supplier = supplier,
            Buyer = buyer,
            TaxExclusiveAmount = totalExcl,
            TaxInclusiveAmount = totalExcl + totalTax,
            TaxTotalAmount = totalTax,
            PayableAmount = totalExcl + totalTax,
            LineExtensionAmount = totalExcl,
            TaxSubtotals = taxSubtotals,
            LineItems = lineItems,
        };

        return JsonSerializer.Serialize(new { invoice = dto }, JsonOptions);
    }

    private async Task<UblPartyDto> BuildBuyerPartyAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var extension = await _eInvoiceDbContext.EInvoiceCustomerExtensions
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (extension == null)
        {
            return new UblPartyDto
            {
                Name = "BUYER (configure CustomerExtension)",
                TIN = "TBC",
                RegistrationNumber = "TBC",
            };
        }

        return new UblPartyDto
        {
            Name = extension.IdValue ?? "Customer",
            TIN = extension.TaxIdentificationNumber ?? "TBC",
            RegistrationNumber = extension.IdValue ?? string.Empty,
            IdType = extension.IdType ?? "BRN",
            SSTRegistrationNumber = extension.SstRegistrationNumber,
            Address = new UblAddressDto
            {
                AddressLines = new List<string> { extension.SstBillingAddress ?? string.Empty },
                City = extension.SstBillingCity ?? string.Empty,
                State = extension.SstBillingState ?? string.Empty,
                PostalCode = extension.SstBillingPostalCode ?? string.Empty,
                CountryCode = extension.SstBillingCountry ?? "MYS",
            },
        };
    }

    private async Task<List<UblInvoiceLineDto>> BuildLineItemsAsync(
        InvoiceBatchPostedEvent batchEvent,
        CancellationToken cancellationToken)
    {
        var taxRules = await _taxDbContext.TaxDeterminationRules
            .Where(r => r.CompanyId == batchEvent.CompanyId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        var taxCode = DetermineTaxCode(taxRules);

        return new List<UblInvoiceLineDto>
        {
            new()
            {
                LineNumber = 1,
                Description = $"Invoice {batchEvent.BatchNumber}",
                Quantity = 1,
                UnitCode = "C62",
                UnitPrice = 0,
                LineExtensionAmount = 0,
                TaxTypeCode = taxCode,
                TaxRatePercent = 0,
                TaxAmount = 0,
                ClassificationCodes = new List<string> { "022" },
            },
        };
    }
}
