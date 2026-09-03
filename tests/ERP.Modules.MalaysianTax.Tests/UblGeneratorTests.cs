// <copyright file="UblGeneratorTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.MalaysianTax.Tests;

public class UblGeneratorTests
{
    private readonly UblGenerator _sut = new();

    // ===== Helper Methods =====

    private static UblPartyDto CreateValidParty(string name = "Test Sdn Bhd") => new()
    {
        Name = name,
        TIN = "C1234567890",
        RegistrationNumber = "202401000001",
        IdType = "BRN",
        Address = new UblAddressDto
        {
            AddressLines = new List<string> { "123 Jalan Test" },
            City = "Kuala Lumpur",
            State = "W.P. Kuala Lumpur",
            PostalCode = "50000",
            CountryCode = "MYS",
        },
    };

    private static UblInvoiceDto CreateValidInvoice() => new()
    {
        InvoiceNumber = "INV-2024-001",
        InvoiceTypeCode = "01",
        IssueDate = DateTime.UtcNow.Date,
        CurrencyCode = "MYR",
        ExchangeRate = 1.0m,
        Supplier = CreateValidParty("Supplier Sdn Bhd"),
        Buyer = CreateValidParty("Buyer Sdn Bhd"),
        TaxExclusiveAmount = 1000m,
        TaxInclusiveAmount = 1060m,
        TaxTotalAmount = 60m,
        PayableAmount = 1060m,
        LineExtensionAmount = 1000m,
        TaxSubtotals = new List<UblTaxSubtotalDto>
        {
            new()
            {
                TaxableAmount = 1000m,
                TaxAmount = 60m,
                TaxTypeCode = "01",
                TaxRatePercent = 6m,
            },
        },
        LineItems = new List<UblInvoiceLineDto>
        {
            new()
            {
                LineNumber = 1,
                Description = "Widget A",
                Quantity = 10,
                UnitCode = "C62",
                UnitPrice = 100m,
                LineExtensionAmount = 1000m,
                TaxTypeCode = "01",
                TaxRatePercent = 6m,
                TaxAmount = 60m,
                ClassificationCodes = new List<string> { "01234567" },
            },
        },
    };

    // ===== Validate Tests =====

    [Fact]
    public void Validate_ValidInvoice_ReturnsNoErrors()
    {
        var dto = CreateValidInvoice();
        var errors = UblGenerator.Validate(dto);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_MissingInvoiceNumber_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.InvoiceNumber = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("InvoiceNumber is required"));
    }

    [Fact]
    public void Validate_InvoiceNumberTooLong_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.InvoiceNumber = new string('X', 51);

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("InvoiceNumber must be 50 characters or less"));
    }

    [Fact]
    public void Validate_MissingInvoiceTypeCode_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.InvoiceTypeCode = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("InvoiceTypeCode is required"));
    }

    [Fact]
    public void Validate_MissingIssueDate_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.IssueDate = default;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("IssueDate is required"));
    }

    [Fact]
    public void Validate_InvalidCurrencyCode_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.CurrencyCode = "MY"; // Only 2 chars

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("CurrencyCode must be a 3-letter ISO code"));
    }

    [Fact]
    public void Validate_MissingSupplierName_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.Supplier.Name = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("Supplier Name is required"));
    }

    [Fact]
    public void Validate_MissingSupplierTin_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.Supplier.TIN = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("Supplier TIN is required"));
    }

    [Fact]
    public void Validate_MissingBuyerRegistrationNumber_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.Buyer.RegistrationNumber = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("Buyer RegistrationNumber is required"));
    }

    [Fact]
    public void Validate_NegativeTaxExclusiveAmount_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.TaxExclusiveAmount = -100m;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("TaxExclusiveAmount must not be negative"));
    }

    [Fact]
    public void Validate_NoTaxSubtotals_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.TaxSubtotals.Clear();

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("At least one TaxSubtotal is required"));
    }

    [Fact]
    public void Validate_NoLineItems_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.LineItems.Clear();

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("At least one InvoiceLine is required"));
    }

    [Fact]
    public void Validate_NonMyrWithoutExchangeRate_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.CurrencyCode = "USD";
        dto.ExchangeRate = 0;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("ExchangeRate must be positive for non-MYR currencies"));
    }

    [Fact]
    public void Validate_LineWithoutDescription_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.LineItems[0].Description = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("Line 1: Description is required"));
    }

    [Fact]
    public void Validate_LineWithoutClassificationCode_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.LineItems[0].ClassificationCodes.Clear();

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("Line 1: At least one ClassificationCode is required"));
    }

    [Fact]
    public void Validate_LineWithoutTaxTypeCode_ReturnsError()
    {
        var dto = CreateValidInvoice();
        dto.LineItems[0].TaxTypeCode = string.Empty;

        var errors = UblGenerator.Validate(dto);

        errors.Should().Contain(e => e.Contains("Line 1: TaxTypeCode is required"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var dto = new UblInvoiceDto(); // All fields default/empty

        var errors = UblGenerator.Validate(dto);

        errors.Should().HaveCountGreaterThan(3);
    }

    // ===== GenerateInvoiceJson Tests =====

    [Fact]
    public void GenerateInvoiceJson_ValidDto_ProducesValidJson()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);

        json.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(json);
        doc.Should().NotBeNull();
    }

    [Fact]
    public void GenerateInvoiceJson_IncludesUBLVersion()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.GetProperty("UBLVersionID").GetString().Should().Be("2.1");
    }

    [Fact]
    public void GenerateInvoiceJson_IncludesInvoiceId()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.GetProperty("ID").GetString().Should().Be("INV-2024-001");
    }

    [Fact]
    public void GenerateInvoiceJson_IncludesSupplierAndBuyer()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.TryGetProperty("AccountingSupplierParty", out var supplier).Should().BeTrue();
        invoice.TryGetProperty("AccountingCustomerParty", out var buyer).Should().BeTrue();
    }

    [Fact]
    public void GenerateInvoiceJson_IncludesMonetaryTotal()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");
        var total = invoice.GetProperty("LegalMonetaryTotal");

        total.TryGetProperty("TaxExclusiveAmount", out _).Should().BeTrue();
        total.TryGetProperty("TaxInclusiveAmount", out _).Should().BeTrue();
        total.TryGetProperty("PayableAmount", out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateInvoiceJson_IncludesTaxTotal()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");
        var taxTotal = invoice.GetProperty("TaxTotal");

        taxTotal.TryGetProperty("TaxAmount", out _).Should().BeTrue();
        taxTotal.TryGetProperty("TaxSubtotal", out var subtotals).Should().BeTrue();
        subtotals.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void GenerateInvoiceJson_IncludesInvoiceLines()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");
        var lines = invoice.GetProperty("InvoiceLine");

        lines.GetArrayLength().Should().Be(1);
        lines[0].GetProperty("ID").GetString().Should().Be("1");
    }

    [Fact]
    public void GenerateInvoiceJson_NonMyr_IncludesExchangeRate()
    {
        var dto = CreateValidInvoice();
        dto.CurrencyCode = "USD";
        dto.ExchangeRate = 4.50m;

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.TryGetProperty("TaxExchangeRate", out var rate).Should().BeTrue();
        rate.GetProperty("CalculationRate").GetDecimal().Should().Be(4.50m);
    }

    [Fact]
    public void GenerateInvoiceJson_Myr_DoesNotIncludeExchangeRate()
    {
        var dto = CreateValidInvoice();

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.TryGetProperty("TaxExchangeRate", out _).Should().BeFalse();
    }

    [Fact]
    public void GenerateInvoiceJson_WithPaymentMeans_IncludesPaymentMeans()
    {
        var dto = CreateValidInvoice();
        dto.PaymentMeansCode = "03";
        dto.PaymentTerms = "Net 30 days";

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.TryGetProperty("PaymentMeans", out var means).Should().BeTrue();
    }

    [Fact]
    public void GenerateInvoiceJson_TruncatesInvoiceNumberTo50Chars()
    {
        var dto = CreateValidInvoice();
        dto.InvoiceNumber = new string('X', 60);

        var json = _sut.GenerateInvoiceJson(dto);
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("invoice");

        invoice.GetProperty("ID").GetString().Should().HaveLength(50);
    }
}
