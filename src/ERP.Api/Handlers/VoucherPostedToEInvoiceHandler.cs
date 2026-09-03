// <copyright file="VoucherPostedToEInvoiceHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Core.Domain.Common;
using ERP.Modules.EInvoice.Application;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.EInvoice.Infrastructure;
using ERP.Modules.MalaysianTax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Api.Handlers;

/// <summary>
/// Handles <see cref="VoucherPostedEvent"/> to generate self-billed e-Invoices for AP vouchers.
/// Per LHDN MyInvois rules, when the buyer pays a supplier, the buyer may issue a self-billed
/// invoice on behalf of the supplier. This applies to payments to agents, foreign suppliers,
/// individual acquisitions, and other LHDN-defined scenarios.
/// Only fires when the vendor has <see cref="EInvoiceVendorExtension.CanIssueSelfBilled"/> = true.
/// </summary>
public sealed class VoucherPostedToEInvoiceHandler : IDomainEventHandler<VoucherPostedEvent>
{
    private readonly EInvoiceDbContext _eInvoiceDbContext;
    private readonly ERP.Modules.MalaysianTax.Infrastructure.MalaysianTaxDbContext _taxDbContext;
    private readonly IEInvoiceService _eInvoiceService;
    private readonly ILogger<VoucherPostedToEInvoiceHandler> _logger;

    public VoucherPostedToEInvoiceHandler(
        EInvoiceDbContext eInvoiceDbContext,
        ERP.Modules.MalaysianTax.Infrastructure.MalaysianTaxDbContext taxDbContext,
        IEInvoiceService eInvoiceService,
        ILogger<VoucherPostedToEInvoiceHandler> logger)
    {
        _eInvoiceDbContext = eInvoiceDbContext;
        _taxDbContext = taxDbContext;
        _eInvoiceService = eInvoiceService;
        _logger = logger;
    }

    public async Task HandleAsync(VoucherPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing VoucherPostedEvent for Voucher {VoucherId}, Vendor {VendorId}, Company {CompanyId}",
            domainEvent.VoucherId,
            domainEvent.VendorId,
            domainEvent.CompanyId);

        // Check if this vendor requires self-billed e-Invoice
        var vendorExtension = await _eInvoiceDbContext.EInvoiceVendorExtensions
            .FirstOrDefaultAsync(v => v.CompanyId == domainEvent.CompanyId
                && v.VendorId == domainEvent.VendorId
                && v.CanIssueSelfBilled
                && !v.IsDeleted,
                cancellationToken);

        if (vendorExtension == null)
        {
            _logger.LogDebug(
                "Vendor {VendorId} not configured for self-billed e-Invoice, skipping.",
                domainEvent.VendorId);
            return;
        }

        // Load supplier party (the vendor — they are the "supplier" in self-billed context)
        var supplier = new UblPartyDto
        {
            Name = vendorExtension.IdValue ?? "Unknown Vendor",
            TIN = vendorExtension.TaxIdentificationNumber ?? "EI00000000010",
            RegistrationNumber = vendorExtension.IdValue ?? string.Empty,
            IdType = vendorExtension.IdType ?? "BRN",
            SSTRegistrationNumber = vendorExtension.SstRegistrationNumber,
            Address = new UblAddressDto
            {
                AddressLines = !string.IsNullOrEmpty(vendorExtension.SstBillingAddress)
                    ? new List<string> { vendorExtension.SstBillingAddress }
                    : new List<string>(),
                City = vendorExtension.SstBillingCity ?? string.Empty,
                State = vendorExtension.SstBillingState ?? string.Empty,
                PostalCode = vendorExtension.SstBillingPostalCode ?? string.Empty,
                CountryCode = "MYS",
            },
        };

        // Load buyer party (our company — TaxpayerProfile)
        var taxpayerProfile = await _eInvoiceDbContext.TaxpayerProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == domainEvent.CompanyId, cancellationToken);

        var buyer = new UblPartyDto
        {
            Name = taxpayerProfile?.BusinessRegistrationNumber ?? "Our Company",
            TIN = taxpayerProfile?.TaxIdentificationNumber ?? "C230601D123456",
            RegistrationNumber = taxpayerProfile?.BusinessRegistrationNumber ?? string.Empty,
            IdType = "BRN",
            SSTRegistrationNumber = taxpayerProfile?.SalesTaxNumber,
            MsicCode = taxpayerProfile?.MsicCode,
            Address = new UblAddressDto
            {
                AddressLines = !string.IsNullOrEmpty(taxpayerProfile?.Address)
                    ? new List<string> { taxpayerProfile!.Address! }
                    : new List<string>(),
                City = taxpayerProfile?.City ?? string.Empty,
                State = taxpayerProfile?.State ?? string.Empty,
                PostalCode = taxpayerProfile?.PostCode ?? string.Empty,
                CountryCode = "MYS",
            },
        };

        // Build line items from voucher cost lines
        var lineItems = new List<UblInvoiceLineDto>();
        var lineNum = 1;
        foreach (var costLine in domainEvent.Lines)
        {
            // Determine tax code from rules
            var taxCode = await ResolveTaxCodeAsync(domainEvent.CompanyId, costLine.Category, cancellationToken);

            lineItems.Add(new UblInvoiceLineDto
            {
                LineNumber = lineNum++,
                Description = costLine.Category ?? $"Voucher {domainEvent.VoucherNumber} Line {lineNum - 1}",
                Quantity = 1,
                UnitCode = "C62",
                UnitPrice = costLine.Amount,
                LineExtensionAmount = costLine.Amount,
                TaxTypeCode = taxCode?.LhdntTaxType ?? "01",
                TaxRatePercent = 0m,
                TaxAmount = 0m,
            });
        }

        var totalAmount = lineItems.Sum(l => l.LineExtensionAmount);

        // Create self-billed invoice
        var selfBilledInvoice = UblInvoiceDto.CreateSelfBilledInvoice(
            supplier: supplier,
            buyer: buyer,
            invoiceNumber: $"SB-{domainEvent.VoucherNumber}",
            totalAmount: totalAmount,
            taxAmount: 0m,
            lineItems: lineItems);

        var payload = System.Text.Json.JsonSerializer.Serialize(selfBilledInvoice);

        try
        {
            await _eInvoiceService.SubmitDocumentAsync(
                companyId: domainEvent.CompanyId,
                sourceDocumentId: domainEvent.VoucherId,
                documentType: "SelfBilledInvoice",
                payload: payload,
                tin: vendorExtension.TaxIdentificationNumber,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Self-billed e-Invoice created and submitted for Voucher {VoucherId}",
                domainEvent.VoucherId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to submit self-billed e-Invoice for Voucher {VoucherId}",
                domainEvent.VoucherId);

            // Don't throw — e-Invoice failure shouldn't block voucher posting
        }
    }

    private async Task<TaxCode?> ResolveTaxCodeAsync(
        Guid companyId,
        string? category,
        CancellationToken ct)
    {
        // Load determination rules for this company
        var rules = await _taxDbContext.TaxDeterminationRules
            .Where(r => r.CompanyId == companyId
                && r.IsActive
                && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= DateTime.UtcNow)
                && (!r.EffectiveTo.HasValue || r.EffectiveTo >= DateTime.UtcNow))
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        // Find matching rule
        foreach (var rule in rules)
        {
            var categoryMatch = string.IsNullOrEmpty(rule.ItemCategory)
                || rule.ItemCategory == "*"
                || string.Equals(rule.ItemCategory, category, StringComparison.OrdinalIgnoreCase);

            if (categoryMatch)
            {
                return await _taxDbContext.TaxCodes
                    .FirstOrDefaultAsync(tc => tc.Id == rule.TaxCodeId, ct);
            }
        }

        // Fallback to SST-SALES-10
        return await _taxDbContext.TaxCodes
            .FirstOrDefaultAsync(tc => tc.Code == "SST-SALES-10", ct);
    }
}
