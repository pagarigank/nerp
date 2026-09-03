// <copyright file="UblGenerator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using ERP.Modules.EInvoice.Domain;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Generates UBL 2.1 JSON payloads compliant with LHDN MyInvois schema.
/// Produces JSON (not XML) since MyInvois API accepts JSON natively.
/// All 37 mandatory fields are mapped per sdk.myinvois.hasil.gov.my/documents/invoice-v1-1/.
/// </summary>
public class UblGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Validates that a UblInvoiceDto has all mandatory fields populated.
    /// Returns a list of validation errors (empty if valid).
    /// </summary>
    public static List<string> Validate(UblInvoiceDto dto)
    {
        var errors = new List<string>();

        // Core
        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            errors.Add("InvoiceNumber is required.");
        if (dto.InvoiceNumber?.Length > 50)
            errors.Add("InvoiceNumber must be 50 characters or less.");
        if (string.IsNullOrWhiteSpace(dto.InvoiceTypeCode))
            errors.Add("InvoiceTypeCode is required.");
        if (dto.IssueDate == default)
            errors.Add("IssueDate is required.");
        if (string.IsNullOrWhiteSpace(dto.CurrencyCode) || dto.CurrencyCode.Length != 3)
            errors.Add("CurrencyCode must be a 3-letter ISO code.");

        // Supplier
        ValidateParty(dto.Supplier, "Supplier", errors);

        // Buyer
        ValidateParty(dto.Buyer, "Buyer", errors);

        // Monetary Total
        if (dto.TaxExclusiveAmount < 0)
            errors.Add("TaxExclusiveAmount must not be negative.");
        if (dto.TaxInclusiveAmount < 0)
            errors.Add("TaxInclusiveAmount must not be negative.");
        if (dto.PayableAmount < 0)
            errors.Add("PayableAmount must not be negative.");

        // Tax Total
        if (dto.TaxSubtotals.Count == 0)
            errors.Add("At least one TaxSubtotal is required.");

        // Line Items
        if (dto.LineItems.Count == 0)
            errors.Add("At least one InvoiceLine is required.");

        foreach (var line in dto.LineItems)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
                errors.Add($"Line {line.LineNumber}: Description is required.");
            if (line.UnitPrice < 0)
                errors.Add($"Line {line.LineNumber}: UnitPrice must not be negative.");
            if (line.ClassificationCodes.Count == 0)
                errors.Add($"Line {line.LineNumber}: At least one ClassificationCode is required.");
            if (string.IsNullOrWhiteSpace(line.TaxTypeCode))
                errors.Add($"Line {line.LineNumber}: TaxTypeCode is required.");
        }

        return errors;
    }

    /// <summary>
    /// Generates a UBL 2.1 Invoice JSON payload from the provided DTO.
    /// Returns a JSON string ready for signing and submission to MyInvois.
    /// </summary>
    public string GenerateInvoiceJson(UblInvoiceDto dto)
    {
        var payload = new Dictionary<string, object>
        {
            // ---- Core (mandatory) ----
            ["invoice"] = new Dictionary<string, object>
            {
                // UBL namespace
                ["_"] = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2",
                ["UBLVersionID"] = "2.1",
                ["CustomizationID"] = "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0",

                // e-Invoice fields
                ["ID"] = Truncate(dto.InvoiceNumber, 50),
                ["IssueDate"] = dto.IssueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["IssueTime"] = dto.IssueTime ?? DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture) + "Z",
                ["InvoiceTypeCode"] = new Dictionary<string, object>
                {
                    ["_"] = dto.InvoiceTypeCode,
                    ["listID"] = dto.InvoiceTypeCode,
                    ["listVersionID"] = "1.1",
                },

                // Currency
                ["DocumentCurrencyCode"] = dto.CurrencyCode,
            },
        };

        var invoice = (Dictionary<string, object>)payload["invoice"];

        // ---- Exchange Rate (mandatory where applicable) ----
        if (dto.ExchangeRate != 1.0m && dto.CurrencyCode != "MYR")
        {
            invoice["TaxExchangeRate"] = new Dictionary<string, object>
            {
                ["CalculationRate"] = dto.ExchangeRate,
                ["SourceCurrencyCode"] = dto.CurrencyCode,
                ["TargetCurrencyCode"] = "MYR",
            };
        }

        // ---- Signature placeholder (mandatory — actual signing happens separately) ----
        invoice["Signature"] = new Dictionary<string, object>
        {
            ["ID"] = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2",
        };

        // ---- Supplier (mandatory) ----
        invoice["AccountingSupplierParty"] = BuildParty(dto.Supplier);

        // ---- Buyer (mandatory) ----
        invoice["AccountingCustomerParty"] = BuildParty(dto.Buyer);

        // ---- Payment Means (optional) ----
        if (!string.IsNullOrEmpty(dto.PaymentMeansCode))
        {
            var paymentMeans = new Dictionary<string, object>
            {
                ["PaymentMeansCode"] = dto.PaymentMeansCode,
            };
            if (!string.IsNullOrEmpty(dto.PaymentTerms))
            {
                paymentMeans["PaymentTerms"] = new List<object>
                {
                    new Dictionary<string, object> { ["Note"] = dto.PaymentTerms },
                };
            }
            invoice["PaymentMeans"] = paymentMeans;
        }

        // ---- Monetary Total (mandatory) ----
        invoice["LegalMonetaryTotal"] = BuildMonetaryTotal(dto);

        // ---- Tax Total (mandatory) ----
        invoice["TaxTotal"] = BuildTaxTotal(dto);

        // ---- Invoice Lines (mandatory) ----
        invoice["InvoiceLine"] = dto.LineItems.Select(BuildInvoiceLine).ToList();

        // ---- Shipping (optional) ----
        if (!string.IsNullOrEmpty(dto.ShippingRecipientName) || dto.ShippingRecipientAddress != null)
        {
            var delivery = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(dto.ShippingRecipientName))
            {
                delivery["DeliveryParty"] = new Dictionary<string, object>
                {
                    ["PartyLegalEntity"] = new Dictionary<string, object>
                    {
                        ["RegistrationName"] = dto.ShippingRecipientName,
                    },
                };
            }
            if (dto.ShippingRecipientAddress != null)
            {
                delivery["DeliveryParty"] ??= new Dictionary<string, object>();
                ((Dictionary<string, object>)delivery["DeliveryParty"])["PostalAddress"] = BuildAddress(dto.ShippingRecipientAddress);
            }
            invoice["Delivery"] = delivery;
        }

        // ---- Billing Reference (for Credit Notes, Debit Notes, Refund Notes) ----
        if (!string.IsNullOrEmpty(dto.BillingReferenceNumber) || !string.IsNullOrEmpty(dto.BillingReferenceUuid))
        {
            var billingRef = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(dto.BillingReferenceNumber))
            {
                billingRef["InvoiceDocumentReference"] = new Dictionary<string, object>
                {
                    ["ID"] = dto.BillingReferenceNumber,
                };
            }
            if (!string.IsNullOrEmpty(dto.BillingReferenceUuid))
            {
                billingRef["InvoiceDocumentReference"] ??= new Dictionary<string, object>();
                ((Dictionary<string, object>)billingRef["InvoiceDocumentReference"])["UUID"] = dto.BillingReferenceUuid;
            }
            invoice["BillingReference"] = new List<object> { billingRef };
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static void ValidateParty(UblPartyDto party, string role, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(party.Name))
            errors.Add($"{role} Name is required.");
        if (party.Name?.Length > 300)
            errors.Add($"{role} Name must be 300 characters or less.");
        if (string.IsNullOrWhiteSpace(party.TIN))
            errors.Add($"{role} TIN is required.");
        if (party.TIN?.Length > 14)
            errors.Add($"{role} TIN must be 14 characters or less.");
        if (string.IsNullOrWhiteSpace(party.RegistrationNumber))
            errors.Add($"{role} RegistrationNumber is required.");
    }

    private static Dictionary<string, object> BuildParty(UblPartyDto party)
    {
        var result = new Dictionary<string, object>
        {
            ["Party"] = new Dictionary<string, object>
            {
                ["PartyIdentification"] = new List<object>
                {
                    new Dictionary<string, object> { ["ID"] = new Dictionary<string, object> { ["_"] = party.TIN, ["schemeID"] = "TIN" } },
                    new Dictionary<string, object> { ["ID"] = new Dictionary<string, object> { ["_"] = party.RegistrationNumber, ["schemeID"] = party.IdType } },
                },
                ["PostalAddress"] = BuildAddress(party.Address),
                ["PartyLegalEntity"] = new Dictionary<string, object>
                {
                    ["RegistrationName"] = party.Name,
                },
            },
        };

        // Add optional fields
        var partyElement = (Dictionary<string, object>)result["Party"];

        if (!string.IsNullOrEmpty(party.SSTRegistrationNumber))
        {
            partyElement["PartyIdentification"] = ((List<object>)partyElement["PartyIdentification"]).ToList();
            ((List<object>)partyElement["PartyIdentification"]).Add(
                new Dictionary<string, object> { ["ID"] = new Dictionary<string, object> { ["_"] = party.SSTRegistrationNumber, ["schemeID"] = "SST" } });
        }

        if (!string.IsNullOrEmpty(party.ContactNumber))
        {
            partyElement["Contact"] = new Dictionary<string, object>
            {
                ["Telephone"] = party.ContactNumber,
            };
        }

        if (!string.IsNullOrEmpty(party.MsicCode))
        {
            partyElement["PartyTaxScheme"] = new Dictionary<string, object>
            {
                ["CompanyID"] = party.MsicCode,
                ["TaxScheme"] = new Dictionary<string, object>
                {
                    ["ID"] = new Dictionary<string, object>
                    {
                        ["_"] = "OTH",
                        ["schemeID"] = "UN/ECE 5153",
                        ["schemeAgencyID"] = "6",
                    },
                },
            };
        }

        if (!string.IsNullOrEmpty(party.BusinessActivityDescription))
        {
            ((Dictionary<string, object>)result["Party"])["IndustryClassificationCode"] = party.BusinessActivityDescription;
        }

        return result;
    }

    private static Dictionary<string, object> BuildAddress(UblAddressDto address)
    {
        var result = new Dictionary<string, object>
        {
            ["Country"] = new Dictionary<string, object>
            {
                ["IdentificationCode"] = address.CountryCode,
            },
        };

        var addressLines = new List<string>();
        addressLines.AddRange(address.AddressLines);
        result["AddressLine"] = addressLines.Select(line => new Dictionary<string, object> { ["Line"] = line }).ToList();

        if (!string.IsNullOrEmpty(address.City))
            result["CityName"] = address.City;
        if (!string.IsNullOrEmpty(address.State))
            result["CountrySubentity"] = address.State;
        if (!string.IsNullOrEmpty(address.PostalCode))
            result["PostalZone"] = address.PostalCode;

        return result;
    }

    private static Dictionary<string, object> BuildMonetaryTotal(UblInvoiceDto dto)
    {
        var result = new Dictionary<string, object>
        {
            ["TaxExclusiveAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.TaxExclusiveAmount), ["currencyID"] = dto.CurrencyCode },
            ["TaxInclusiveAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.TaxInclusiveAmount), ["currencyID"] = dto.CurrencyCode },
            ["PayableAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.PayableAmount), ["currencyID"] = dto.CurrencyCode },
            ["LineExtensionAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.LineExtensionAmount), ["currencyID"] = dto.CurrencyCode },
        };

        if (dto.AllowanceTotalAmount.HasValue)
            result["AllowanceTotalAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.AllowanceTotalAmount.Value), ["currencyID"] = dto.CurrencyCode };
        if (dto.ChargeTotalAmount.HasValue)
            result["ChargeTotalAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.ChargeTotalAmount.Value), ["currencyID"] = dto.CurrencyCode };
        if (dto.PayableRoundingAmount.HasValue)
            result["PayableRoundingAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.PayableRoundingAmount.Value), ["currencyID"] = dto.CurrencyCode };

        return result;
    }

    private static Dictionary<string, object> BuildTaxTotal(UblInvoiceDto dto)
    {
        var result = new Dictionary<string, object>
        {
            ["TaxAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(dto.TaxTotalAmount), ["currencyID"] = dto.CurrencyCode },
            ["TaxSubtotal"] = dto.TaxSubtotals.Select(st => BuildTaxSubtotal(st, dto.CurrencyCode)).ToList(),
        };

        return result;
    }

    private static Dictionary<string, object> BuildTaxSubtotal(UblTaxSubtotalDto st, string currencyCode)
    {
        var subtotal = new Dictionary<string, object>
        {
            ["TaxableAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(st.TaxableAmount), ["currencyID"] = currencyCode },
            ["TaxAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(st.TaxAmount), ["currencyID"] = currencyCode },
            ["TaxCategory"] = new Dictionary<string, object>
            {
                ["ID"] = st.TaxTypeCode,
                ["TaxScheme"] = new Dictionary<string, object>
                {
                    ["ID"] = new Dictionary<string, object>
                    {
                        ["_"] = "OTH",
                        ["schemeID"] = "UN/ECE 5153",
                        ["schemeAgencyID"] = "6",
                    },
                },
            },
        };

        if (st.TaxRatePercent > 0)
            ((Dictionary<string, object>)subtotal["TaxCategory"])["Percent"] = st.TaxRatePercent;

        if (!string.IsNullOrEmpty(st.TaxExemptionReason))
        {
            ((Dictionary<string, object>)subtotal["TaxCategory"])["TaxExemptionReason"] = st.TaxExemptionReason;
        }

        return subtotal;
    }

    private static Dictionary<string, object> BuildInvoiceLine(UblInvoiceLineDto line)
    {
        var result = new Dictionary<string, object>
        {
            ["ID"] = line.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["InvoicedQuantity"] = new Dictionary<string, object>
            {
                ["_"] = FormatDecimal(line.Quantity),
                ["unitCode"] = line.UnitCode,
            },
            ["LineExtensionAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.LineExtensionAmount), ["currencyID"] = "MYR" },
            ["Item"] = new Dictionary<string, object>
            {
                ["Description"] = line.Description,
                ["CommodityClassification"] = line.ClassificationCodes.Select(cc => new Dictionary<string, object>
                {
                    ["ItemClassificationCode"] = new Dictionary<string, object> { ["_"] = cc, ["listID"] = "CLASS" },
                }).ToList(),
            },
            ["Price"] = new Dictionary<string, object>
            {
                ["PriceAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.UnitPrice), ["currencyID"] = "MYR" },
            },
            ["TaxTotal"] = new Dictionary<string, object>
            {
                ["TaxAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.TaxAmount), ["currencyID"] = "MYR" },
                ["TaxSubtotal"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["TaxableAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.LineExtensionAmount), ["currencyID"] = "MYR" },
                        ["TaxAmount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.TaxAmount), ["currencyID"] = "MYR" },
                        ["TaxCategory"] = new Dictionary<string, object>
                        {
                            ["ID"] = line.TaxTypeCode,
                            ["Percent"] = line.TaxRatePercent,
                            ["TaxScheme"] = new Dictionary<string, object>
                            {
                                ["ID"] = new Dictionary<string, object>
                                {
                                    ["_"] = "OTH",
                                    ["schemeID"] = "UN/ECE 5153",
                                    ["schemeAgencyID"] = "6",
                                },
                            },
                        },
                    },
                },
            },
            ["ItemPriceExtension"] = new Dictionary<string, object>
            {
                ["Amount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.LineExtensionAmount), ["currencyID"] = "MYR" },
            },
        };

        // Optional fields
        var itemElement = (Dictionary<string, object>)result["Item"];

        if (!string.IsNullOrEmpty(line.ProductTariffCode))
        {
            ((List<object>)itemElement["CommodityClassification"]).Add(
                new Dictionary<string, object> { ["ItemClassificationCode"] = new Dictionary<string, object> { ["_"] = line.ProductTariffCode, ["listID"] = "PTC" } });
        }

        if (!string.IsNullOrEmpty(line.CountryOfOrigin))
        {
            itemElement["OriginCountry"] = new Dictionary<string, object>
            {
                ["IdentificationCode"] = line.CountryOfOrigin,
            };
        }

        // Discounts/Charges
        if (line.DiscountAmount.HasValue || line.DiscountRate.HasValue)
        {
            var allowance = new Dictionary<string, object>
            {
                ["ChargeIndicator"] = false,
            };
            if (line.DiscountAmount.HasValue)
                allowance["Amount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.DiscountAmount.Value), ["currencyID"] = "MYR" };
            if (line.DiscountRate.HasValue)
                allowance["MultiplierFactorNumeric"] = line.DiscountRate.Value;
            if (!string.IsNullOrEmpty(line.DiscountReason))
                allowance["AllowanceChargeReason"] = line.DiscountReason;

            result["AllowanceCharge"] = new List<object> { allowance };
        }

        if (line.ChargeAmount.HasValue || line.ChargeRate.HasValue)
        {
            var charge = new Dictionary<string, object>
            {
                ["ChargeIndicator"] = true,
            };
            if (line.ChargeAmount.HasValue)
                charge["Amount"] = new Dictionary<string, object> { ["_"] = FormatDecimal(line.ChargeAmount.Value), ["currencyID"] = "MYR" };
            if (line.ChargeRate.HasValue)
                charge["MultiplierFactorNumeric"] = line.ChargeRate.Value;
            if (!string.IsNullOrEmpty(line.ChargeReason))
                charge["AllowanceChargeReason"] = line.ChargeReason;

            var allowances = result.ContainsKey("AllowanceCharge")
                ? (List<object>)result["AllowanceCharge"]
                : new List<object>();
            allowances.Add(charge);
            result["AllowanceCharge"] = allowances;
        }

        return result;
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
