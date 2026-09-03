// <copyright file="QRCodeService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.EInvoice.Infrastructure;

/// <summary>
/// Generates QR code payloads for MyInvois e-Invoice documents.
/// The QR code encodes a JSON payload with key document fields per LHDN specification.
/// QR image generation is delegated to the client (frontend) or a separate QRCoder service.
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// Generates the QR code JSON payload for a validated e-Invoice document.
    /// </summary>
    Task<string?> GenerateQrPayloadAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Generates QR payload from document data without requiring a DB lookup.
    /// </summary>
    string GenerateQrPayload(
        string myInvoisUuid,
        string validationLink,
        string supplierName,
        string supplierTin,
        string supplierRegistrationNumber,
        string buyerName,
        string buyerTin,
        string buyerRegistrationNumber,
        string invoiceNumber,
        DateTime issueDate,
        decimal totalExcludingTax,
        decimal totalIncludingTax,
        string currencyCode);
}

public class QRCodeService : IQRCodeService
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly ILogger<QRCodeService> _logger;

    public QRCodeService(EInvoiceDbContext dbContext, ILogger<QRCodeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string?> GenerateQrPayloadAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await _dbContext.EInvoiceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found for QR generation", documentId);
            return null;
        }

        if (string.IsNullOrEmpty(document.MyInvoisUuid) || string.IsNullOrEmpty(document.ValidationLink))
        {
            _logger.LogWarning(
                "Document {DocumentId} is missing UUID or validation link — cannot generate QR payload",
                documentId);
            return null;
        }

        var payload = GenerateQrPayload(
            document.MyInvoisUuid,
            document.ValidationLink,
            document.SupplierTIN ?? string.Empty,
            string.Empty, // Supplier name — not stored on EInvoiceDocument
            string.Empty, // Supplier RegNo — not stored on EInvoiceDocument
            string.Empty, // Buyer name — not stored on EInvoiceDocument
            document.CustomerTIN ?? string.Empty,
            string.Empty, // Buyer RegNo — not stored on EInvoiceDocument
            document.SourceDocumentType,
            document.SubmittedAt ?? document.CreatedAt,
            document.TotalAmount - document.TaxAmount,
            document.TotalAmount,
            document.CurrencyCode);

        // Store the payload on the document
        document.QrCodePayload = payload;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Generated QR payload for document {DocumentId}, UUID={Uuid}",
            documentId,
            document.MyInvoisUuid);

        return payload;
    }

    /// <summary>
    /// Generates the QR code JSON payload per LHDN MyInvois QR specification.
    /// Format: JSON with fields matching the official QR data structure.
    /// </summary>
    public string GenerateQrPayload(
        string myInvoisUuid,
        string validationLink,
        string supplierName,
        string supplierTin,
        string supplierRegistrationNumber,
        string buyerName,
        string buyerTin,
        string buyerRegistrationNumber,
        string invoiceNumber,
        DateTime issueDate,
        decimal totalExcludingTax,
        decimal totalIncludingTax,
        string currencyCode)
    {
        var qrData = new Dictionary<string, object>
        {
            ["uuid"] = myInvoisUuid,
            ["validationLink"] = validationLink,
            ["supplier"] = new Dictionary<string, string>
            {
                ["name"] = Truncate(supplierName, 300),
                ["tin"] = supplierTin,
                ["registrationNumber"] = supplierRegistrationNumber,
            },
            ["buyer"] = new Dictionary<string, string>
            {
                ["name"] = Truncate(buyerName, 300),
                ["tin"] = buyerTin,
                ["registrationNumber"] = buyerRegistrationNumber,
            },
            ["invoiceNumber"] = Truncate(invoiceNumber, 50),
            ["issueDate"] = issueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ["totalExcludingTax"] = FormatDecimal(totalExcludingTax),
            ["totalIncludingTax"] = FormatDecimal(totalIncludingTax),
            ["currencyCode"] = currencyCode,
        };

        return JsonSerializer.Serialize(qrData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
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
