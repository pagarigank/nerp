// <copyright file="LhdnErrorMapper.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Maps LHDN MyInvois error codes and raw error messages to plain-language descriptions
/// that end users can understand and act on.
/// </summary>
public static class LhdnErrorMapper
{
    private static readonly Dictionary<string, string> ErrorCodeMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        // TIN validation errors
        ["INVALID_TIN"] = "The Taxpayer Identification Number (TIN) is invalid or not registered with LHDN. Please verify the TIN with the taxpayer.",
        ["TIN_NOT_FOUND"] = "The TIN was not found in LHDN's database. The taxpayer may not be registered for e-Invoicing.",
        ["TIN_INACTIVE"] = "The TIN is inactive. The taxpayer's registration may have been suspended or revoked.",

        // Submission errors
        ["DUPLICATE_SUBMISSION"] = "This document has already been submitted. Check the existing submission status before resubmitting.",
        ["INVALID_DOCUMENT_TYPE"] = "The document type code is not valid. Ensure you are using a supported type (01=Invoice, 02=Credit Note, 03=Debit Note, 04=Refund Note, 11=Self-Billed).",
        ["INVALID_CURRENCY"] = "The currency code is not valid. Use a standard ISO 4217 currency code (e.g., MYR, USD).",
        ["INVALID_DATE"] = "The issue date is invalid. It must be a valid date in YYYY-MM-DD format and cannot be in the future.",
        ["MISSING_MANDATORY_FIELD"] = "A mandatory field is missing. Please check all required fields are populated.",

        // Amount errors
        ["AMOUNT_MISMATCH"] = "The total amounts do not balance. TaxExclusive + TaxTotal must equal TaxInclusive.",
        ["NEGATIVE_AMOUNT"] = "An amount field contains a negative value where it is not allowed.",
        ["INVALID_TAX_RATE"] = "The tax rate is invalid. Check the tax type code and rate combination.",

        // Address errors
        ["INVALID_STATE"] = "The state code is not valid. Use a Malaysian state code (e.g., KUL, JHR, PNG).",
        ["INVALID_COUNTRY"] = "The country code is not valid. Use ISO 3166-1 alpha-3 (e.g., MYS, SGP).",
        ["INVALID_POSTCODE"] = "The postcode is not valid for the specified state.",

        // Classification errors
        ["INVALID_CLASSIFICATION"] = "The item classification code is not valid. Check the classification list (CLASS).",
        ["INVALID_MSIC"] = "The MSIC (Malaysia Standard Industrial Classification) code is not valid.",
        ["INVALID_TARIFF_CODE"] = "The product tariff code (HS code) is not valid.",

        // Certificate errors
        ["CERTIFICATE_EXPIRED"] = "The digital certificate has expired. Please upload a new certificate.",
        ["CERTIFICATE_INVALID"] = "The digital certificate is invalid or corrupted. Please re-upload a valid PFX certificate.",
        ["SIGNING_FAILED"] = "Document signing failed. Check that the certificate has a valid private key.",

        // Cancellation errors
        ["CANCEL_WINDOW_EXPIRED"] = "The 72-hour cancellation window has expired. Documents older than 72 hours cannot be cancelled.",
        ["ALREADY_CANCELLED"] = "This document has already been cancelled.",
        ["INVALID_CANCEL_REASON"] = "The cancellation reason is required and must be between 10 and 200 characters.",

        // System errors
        ["API_RATE_LIMIT"] = "Too many requests to MyInvois API. Please wait and try again.",
        ["API_UNAVAILABLE"] = "The MyInvois API is temporarily unavailable. Please try again later.",
        ["NETWORK_ERROR"] = "A network error occurred while communicating with MyInvois. Check your internet connection.",
    };

    /// <summary>
    /// Maps an LHDN error code to a plain-language message.
    /// Returns the original error message if no mapping is found.
    /// </summary>
    public static string MapError(string? errorCode, string? rawErrorMessage)
    {
        if (!string.IsNullOrEmpty(errorCode) && ErrorCodeMessages.TryGetValue(errorCode, out var mapped))
        {
            return $"[{errorCode}] {mapped}";
        }

        // Try to match common patterns in the raw message
        if (!string.IsNullOrEmpty(rawErrorMessage))
        {
            var lowerMsg = rawErrorMessage.ToLowerInvariant();

            if (lowerMsg.Contains("tin", StringComparison.Ordinal) && (lowerMsg.Contains("invalid", StringComparison.Ordinal) || lowerMsg.Contains("not found", StringComparison.Ordinal)))
                return $"TIN validation failed: {rawErrorMessage}";

            if (lowerMsg.Contains("duplicate", StringComparison.Ordinal) || lowerMsg.Contains("already submitted", StringComparison.Ordinal))
                return $"Duplicate submission: {rawErrorMessage}";

            if (lowerMsg.Contains("cancel", StringComparison.Ordinal) && lowerMsg.Contains("window", StringComparison.Ordinal))
                return $"Cancellation window expired: {rawErrorMessage}";

            if (lowerMsg.Contains("certificate", StringComparison.Ordinal) || lowerMsg.Contains("signing", StringComparison.Ordinal))
                return $"Certificate issue: {rawErrorMessage}";

            if (lowerMsg.Contains("mandatory", StringComparison.Ordinal) || lowerMsg.Contains("required", StringComparison.Ordinal))
                return $"Missing required field: {rawErrorMessage}";
        }

        // Fallback: return the raw message or a generic message
        return !string.IsNullOrEmpty(rawErrorMessage)
            ? rawErrorMessage
            : "An unknown error occurred. Please check the document and try again.";
    }

    /// <summary>
    /// Returns a user-friendly summary of all validation errors for a document.
    /// </summary>
    public static List<string> MapErrors(List<string>? errorCodes, List<string>? rawMessages)
    {
        var results = new List<string>();

        if (errorCodes != null)
        {
            foreach (var code in errorCodes)
            {
                results.Add(MapError(code, null));
            }
        }

        if (rawMessages != null)
        {
            foreach (var msg in rawMessages)
            {
                if (!results.Contains(msg))
                {
                    results.Add(MapError(null, msg));
                }
            }
        }

        return results.Count > 0 ? results : new List<string> { "No errors found." };
    }
}
