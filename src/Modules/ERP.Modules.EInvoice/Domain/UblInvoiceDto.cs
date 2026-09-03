// <copyright file="UblInvoiceDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Domain;

/// <summary>
/// UBL 2.1 Invoice DTO matching LHDN MyInvois mandatory fields.
/// This is the input model for the UBL generator — populated from ERP data.
/// </summary>
public class UblInvoiceDto
{
    // ---- Core Fields (mandatory) ----

    /// <summary>Gets or sets the e-Invoice type code (01=Invoice, 02=Credit Note, 11=Self-Billed, etc.).</summary>
    public string InvoiceTypeCode { get; set; } = "01";

    /// <summary>Gets or sets the document reference number (max 50 chars).</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the date of issuance (must be current UTC date).</summary>
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>Gets or sets the time of issuance (UTC).</summary>
    public string IssueTime { get; set; } = DateTime.UtcNow.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z";

    /// <summary>Gets or sets the invoice currency code (3-letter ISO, default MYR).</summary>
    public string CurrencyCode { get; set; } = "MYR";

    /// <summary>Gets or sets the exchange rate (1.0 for MYR, otherwise the rate to MYR).</summary>
    public decimal ExchangeRate { get; set; } = 1.0m;

    // ---- Supplier (mandatory) ----

    /// <summary>Gets or sets the supplier information.</summary>
    public UblPartyDto Supplier { get; set; } = new();

    // ---- Buyer (mandatory) ----

    /// <summary>Gets or sets the buyer information.</summary>
    public UblPartyDto Buyer { get; set; } = new();

    // ---- Monetary Total (mandatory) ----

    /// <summary>Gets or sets the total excluding tax.</summary>
    public decimal TaxExclusiveAmount { get; set; }

    /// <summary>Gets or sets the total including tax.</summary>
    public decimal TaxInclusiveAmount { get; set; }

    /// <summary>Gets or sets the total payable amount.</summary>
    public decimal PayableAmount { get; set; }

    /// <summary>Gets or sets the total net amount (line extensions sum).</summary>
    public decimal LineExtensionAmount { get; set; }

    /// <summary>Gets or sets the total discount amount.</summary>
    public decimal? AllowanceTotalAmount { get; set; }

    /// <summary>Gets or sets the total charge amount.</summary>
    public decimal? ChargeTotalAmount { get; set; }

    /// <summary>Gets or sets the rounding adjustment.</summary>
    public decimal? PayableRoundingAmount { get; set; }

    // ---- Tax Total (mandatory) ----

    /// <summary>Gets or sets the total tax amount.</summary>
    public decimal TaxTotalAmount { get; set; }

    /// <summary>Gets or sets the tax subtotals by tax type.</summary>
    public List<UblTaxSubtotalDto> TaxSubtotals { get; set; } = new();

    // ---- Line Items (mandatory) ----

    /// <summary>Gets or sets the invoice line items.</summary>
    public List<UblInvoiceLineDto> LineItems { get; set; } = new();

    // ---- Payment (optional) ----

    /// <summary>Gets or sets the payment means code (01=Cash, 02=Cheque, 03=Credit Transfer, etc.).</summary>
    public string? PaymentMeansCode { get; set; }

    /// <summary>Gets or sets the payment terms description.</summary>
    public string? PaymentTerms { get; set; }

    // ---- Shipping (optional) ----

    /// <summary>Gets or sets the shipping recipient name.</summary>
    public string? ShippingRecipientName { get; set; }

    /// <summary>Gets or sets the shipping recipient address.</summary>
    public UblAddressDto? ShippingRecipientAddress { get; set; }

    // ---- Document References (optional) ----

    /// <summary>Gets or sets the billing reference number (original invoice number for credit/debit notes).</summary>
    public string? BillingReferenceNumber { get; set; }

    /// <summary>Gets or sets the billing reference UUID (original invoice MyInvois UUID for credit/debit/refund notes).</summary>
    public string? BillingReferenceUuid { get; set; }

    /// <summary>Gets or sets customs import form references (comma-separated).</summary>
    public string? CustomsFormReferences { get; set; }

    /// <summary>
    /// Creates a Credit Note DTO from an original invoice DTO.
    /// </summary>
    public static UblInvoiceDto CreateCreditNote(
        UblInvoiceDto originalInvoice,
        string creditNoteNumber,
        decimal creditAmount,
        string reason)
    {
        return new UblInvoiceDto
        {
            InvoiceTypeCode = "02",
            InvoiceNumber = creditNoteNumber,
            IssueDate = DateTime.UtcNow.Date,
            IssueTime = DateTime.UtcNow.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z",
            CurrencyCode = originalInvoice.CurrencyCode,
            ExchangeRate = originalInvoice.ExchangeRate,
            Supplier = originalInvoice.Supplier,
            Buyer = originalInvoice.Buyer,
            TaxExclusiveAmount = creditAmount,
            TaxInclusiveAmount = creditAmount,
            PayableAmount = creditAmount,
            LineExtensionAmount = creditAmount,
            BillingReferenceNumber = originalInvoice.InvoiceNumber,
            BillingReferenceUuid = originalInvoice.BillingReferenceUuid,
            LineItems = new List<UblInvoiceLineDto>
            {
                new()
                {
                    LineNumber = 1,
                    Description = $"Credit Note: {reason}",
                    Quantity = 1,
                    UnitCode = "C62",
                    UnitPrice = creditAmount,
                    LineExtensionAmount = creditAmount,
                }
            }
        };
    }

    /// <summary>
    /// Creates a Debit Note DTO from an original invoice DTO.
    /// </summary>
    public static UblInvoiceDto CreateDebitNote(
        UblInvoiceDto originalInvoice,
        string debitNoteNumber,
        decimal debitAmount,
        string reason)
    {
        return new UblInvoiceDto
        {
            InvoiceTypeCode = "03",
            InvoiceNumber = debitNoteNumber,
            IssueDate = DateTime.UtcNow.Date,
            IssueTime = DateTime.UtcNow.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z",
            CurrencyCode = originalInvoice.CurrencyCode,
            ExchangeRate = originalInvoice.ExchangeRate,
            Supplier = originalInvoice.Supplier,
            Buyer = originalInvoice.Buyer,
            TaxExclusiveAmount = debitAmount,
            TaxInclusiveAmount = debitAmount,
            PayableAmount = debitAmount,
            LineExtensionAmount = debitAmount,
            BillingReferenceNumber = originalInvoice.InvoiceNumber,
            BillingReferenceUuid = originalInvoice.BillingReferenceUuid,
            LineItems = new List<UblInvoiceLineDto>
            {
                new()
                {
                    LineNumber = 1,
                    Description = $"Debit Note: {reason}",
                    Quantity = 1,
                    UnitCode = "C62",
                    UnitPrice = debitAmount,
                    LineExtensionAmount = debitAmount,
                }
            }
        };
    }

    /// <summary>
    /// Creates a Refund Note DTO from an original invoice DTO.
    /// </summary>
    public static UblInvoiceDto CreateRefundNote(
        UblInvoiceDto originalInvoice,
        string refundNoteNumber,
        decimal refundAmount,
        string reason)
    {
        return new UblInvoiceDto
        {
            InvoiceTypeCode = "04",
            InvoiceNumber = refundNoteNumber,
            IssueDate = DateTime.UtcNow.Date,
            IssueTime = DateTime.UtcNow.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z",
            CurrencyCode = originalInvoice.CurrencyCode,
            ExchangeRate = originalInvoice.ExchangeRate,
            Supplier = originalInvoice.Supplier,
            Buyer = originalInvoice.Buyer,
            TaxExclusiveAmount = refundAmount,
            TaxInclusiveAmount = refundAmount,
            PayableAmount = refundAmount,
            LineExtensionAmount = refundAmount,
            BillingReferenceNumber = originalInvoice.InvoiceNumber,
            BillingReferenceUuid = originalInvoice.BillingReferenceUuid,
            LineItems = new List<UblInvoiceLineDto>
            {
                new()
                {
                    LineNumber = 1,
                    Description = $"Refund Note: {reason}",
                    Quantity = 1,
                    UnitCode = "C62",
                    UnitPrice = refundAmount,
                    LineExtensionAmount = refundAmount,
                }
            }
        };
    }

    /// <summary>
    /// Creates a Self-Billed Invoice DTO.
    /// InvoiceTypeCode=11 per LHDN MyInvois specification.
    /// Used when the buyer issues the invoice on behalf of the supplier.
    /// </summary>
    public static UblInvoiceDto CreateSelfBilledInvoice(
        UblPartyDto supplier,
        UblPartyDto buyer,
        string invoiceNumber,
        decimal totalAmount,
        decimal taxAmount,
        List<UblInvoiceLineDto> lineItems,
        string currencyCode = "MYR")
    {
        return new UblInvoiceDto
        {
            InvoiceTypeCode = "11",
            InvoiceNumber = invoiceNumber,
            IssueDate = DateTime.UtcNow.Date,
            IssueTime = DateTime.UtcNow.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "Z",
            CurrencyCode = currencyCode,
            ExchangeRate = 1.0m,
            Supplier = supplier,
            Buyer = buyer,
            TaxExclusiveAmount = totalAmount,
            TaxInclusiveAmount = totalAmount + taxAmount,
            TaxTotalAmount = taxAmount,
            PayableAmount = totalAmount + taxAmount,
            LineExtensionAmount = totalAmount,
            LineItems = lineItems,
        };
    }
}

/// <summary>
/// UBL Party information (Supplier or Buyer).
/// </summary>
public class UblPartyDto
{
    /// <summary>Gets or sets the party name (max 300 chars).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the TIN (max 14 chars).</summary>
    public string TIN { get; set; } = string.Empty;

    /// <summary>Gets or sets the registration/identification number.</summary>
    public string RegistrationNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID type (BRN, NRIC, ARMY, PASSPORT, TIN).</summary>
    public string IdType { get; set; } = "BRN";

    /// <summary>Gets or sets the SST registration number (if applicable).</summary>
    public string? SSTRegistrationNumber { get; set; }

    /// <summary>Gets or sets the contact number.</summary>
    public string? ContactNumber { get; set; }

    /// <summary>Gets or sets the MSIC code (5-digit).</summary>
    public string? MsicCode { get; set; }

    /// <summary>Gets or sets the business activity description.</summary>
    public string? BusinessActivityDescription { get; set; }

    /// <summary>Gets or sets the address.</summary>
    public UblAddressDto Address { get; set; } = new();
}

/// <summary>
/// UBL Address structure.
/// </summary>
public class UblAddressDto
{
    /// <summary>Gets or sets the address lines (up to 2 lines).</summary>
    public List<string> AddressLines { get; set; } = new();

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the state/province.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the country code (ISO 3166-1 alpha-3, default MYS).</summary>
    public string CountryCode { get; set; } = "MYS";
}

/// <summary>
/// UBL Tax Subtotal per tax type.
/// </summary>
public class UblTaxSubtotalDto
{
    /// <summary>Gets or sets the taxable amount for this tax type.</summary>
    public decimal TaxableAmount { get; set; }

    /// <summary>Gets or sets the tax amount for this tax type.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the tax type code (01=Sales Tax, 02=Service Tax, etc.).</summary>
    public string TaxTypeCode { get; set; } = "01";

    /// <summary>Gets or sets the tax rate percentage.</summary>
    public decimal TaxRatePercent { get; set; }

    /// <summary>Gets or sets the tax exemption reason (if applicable).</summary>
    public string? TaxExemptionReason { get; set; }

    /// <summary>Gets or sets the exempted amount (if applicable).</summary>
    public decimal? TaxExemptionAmount { get; set; }
}

/// <summary>
/// UBL Invoice Line Item.
/// </summary>
public class UblInvoiceLineDto
{
    /// <summary>Gets or sets the line number (sequential).</summary>
    public int LineNumber { get; set; }

    /// <summary>Gets or sets the item description (max 300 chars).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity.</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Gets or sets the unit of measure code (UN/ECE Rec 20, e.g., C62=Each, KGM=Kilogram).</summary>
    public string UnitCode { get; set; } = "C62";

    /// <summary>Gets or sets the unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the line total excluding tax.</summary>
    public decimal LineExtensionAmount { get; set; }

    /// <summary>Gets or sets the classification codes for this item.</summary>
    public List<string> ClassificationCodes { get; set; } = new();

    /// <summary>Gets or sets the product tariff code (HS code for goods).</summary>
    public string? ProductTariffCode { get; set; }

    /// <summary>Gets or sets the country of origin (ISO 3166-1 alpha-3).</summary>
    public string? CountryOfOrigin { get; set; }

    // ---- Per-line Tax ----

    /// <summary>Gets or sets the tax type code for this line.</summary>
    public string TaxTypeCode { get; set; } = "01";

    /// <summary>Gets or sets the tax rate for this line.</summary>
    public decimal TaxRatePercent { get; set; }

    /// <summary>Gets or sets the tax amount for this line.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the tax exemption reason (if applicable).</summary>
    public string? TaxExemptionReason { get; set; }

    /// <summary>Gets or sets the exempted amount (if applicable).</summary>
    public decimal? TaxExemptionAmount { get; set; }

    // ---- Per-line Discounts/Charges (optional) ----

    /// <summary>Gets or sets the discount rate.</summary>
    public decimal? DiscountRate { get; set; }

    /// <summary>Gets or sets the discount amount.</summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>Gets or sets the discount reason.</summary>
    public string? DiscountReason { get; set; }

    /// <summary>Gets or sets the charge rate.</summary>
    public decimal? ChargeRate { get; set; }

    /// <summary>Gets or sets the charge amount.</summary>
    public decimal? ChargeAmount { get; set; }

    /// <summary>Gets or sets the charge reason.</summary>
    public string? ChargeReason { get; set; }
}
