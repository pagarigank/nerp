// <copyright file="OutboundEdiGenerator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Threading;
using ERP.Modules.Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Integration.Infrastructure;

/// <summary>
/// Generates outbound EDI documents (810 invoice, 856 ASN) from posted
/// AR invoices and confirmed OM shipments. Called by domain event handlers
/// when InvoiceBatchPosted or ShipmentConfirmed events fire.
/// </summary>
public class OutboundEdiGenerator
{
    private readonly IntegrationDbContext _dbContext;
    private readonly ILogger<OutboundEdiGenerator> _logger;
    private int _controlNumberCounter = 1;

    public OutboundEdiGenerator(
        IntegrationDbContext dbContext,
        ILogger<OutboundEdiGenerator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Generate EDI 810 (Invoice) from a posted AR invoice batch.
    /// </summary>
    /// <param name="companyId">Company ID.</param>
    /// <param name="batchId">AR InvoiceBatch ID.</param>
    /// <param name="batchNumber">Batch number.</param>
    /// <param name="postingDate">Posting date.</param>
    /// <param name="currencyCode">Currency code.</param>
    /// <param name="partnerCode">Trading partner code (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generated EDI content and transaction log ID.</returns>
    public async Task<OutboundEdiResult> GenerateInvoice810Async(
        Guid companyId,
        Guid batchId,
        string batchNumber,
        DateTimeOffset postingDate,
        string? currencyCode,
        string? partnerCode = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating EDI 810 for batch {BatchNumber}, CompanyId={CompanyId}",
            batchNumber, companyId);

        // Find the trading partner mapping
        var partner = await FindPartnerAsync(companyId, partnerCode, "810", ct);

        var controlNumber = Interlocked.Increment(ref _controlNumberCounter);

        var doc = new EdiOutboundDocument
        {
            DocumentTypeCode = "810",
            IsaControlNumber = controlNumber,
            GsControlNumber = controlNumber,
            StControlNumber = 1,
            IsaSenderQualifier = "ZZ",
            SenderId = partner?.GsSenderId ?? "ERP-SENDER",
            ReceiverId = partner?.GsReceiverId ?? "ERP-RECEIVER",
            IsaReceiverQualifier = "ZZ",
            FunctionalId = "IN",
            GroupControlNumber = controlNumber.ToString(),
            Version = "005010",

            // 810-specific
            InvoiceNumber = batchNumber,
            InvoiceDate = postingDate.DateTime,
            PoNumber = batchNumber, // Use batch number as PO reference

            // Parties
            SupplierName = "ERP Company",
            SupplierCode = "ERP",
            BuyerName = partner?.Name ?? "CUSTOMER",
            BuyerCode = partner?.PartnerCode ?? "CUST",
        };

        // TODO: Load actual invoice lines from ArDbContext when line-level
        // EDI generation is needed. For now, generate with batch-level totals.

        try
        {
            var ediContent = EdiX12Translator.Generate(doc);

            // Log the outbound transaction
            var transactionLog = new EdiTransactionLog(
                companyId,
                partner?.Id,
                partner?.PartnerCode ?? partnerCode ?? "UNKNOWN",
                "810",
                "Outbound",
                controlNumber.ToString(),
                controlNumber.ToString(),
                "1",
                ediContent.Length > 8000 ? ediContent[..8000] : ediContent);

            transactionLog.MarkProcessed(batchId);
            _dbContext.EdiTransactionLogs.Add(transactionLog);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "EDI 810 generated: Batch={BatchNumber}, Partner={Partner}, {Length} chars",
                batchNumber, partner?.PartnerCode ?? "unknown", ediContent.Length);

            return OutboundEdiResult.Ok(ediContent, transactionLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate EDI 810 for batch {BatchNumber}",
                batchNumber);

            return OutboundEdiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Generate EDI 856 (ASN/Shipment) from a confirmed OM shipment.
    /// </summary>
    /// <param name="companyId">Company ID.</param>
    /// <param name="shipmentId">OM Shipment ID.</param>
    /// <param name="shipmentNumber">Shipment number.</param>
    /// <param name="shipmentDate">Shipment date.</param>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="carrier">Carrier name.</param>
    /// <param name="trackingNumber">Tracking number.</param>
    /// <param name="partnerCode">Trading partner code (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generated EDI content and transaction log ID.</returns>
    public async Task<OutboundEdiResult> GenerateShipment856Async(
        Guid companyId,
        Guid shipmentId,
        string shipmentNumber,
        DateTime shipmentDate,
        Guid customerId,
        string? carrier,
        string? trackingNumber,
        string? partnerCode = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating EDI 856 for shipment {ShipmentNumber}, CompanyId={CompanyId}",
            shipmentNumber, companyId);

        var partner = await FindPartnerAsync(companyId, partnerCode, "856", ct);

        var controlNumber = Interlocked.Increment(ref _controlNumberCounter);

        var doc = new EdiOutboundDocument
        {
            DocumentTypeCode = "856",
            IsaControlNumber = controlNumber,
            GsControlNumber = controlNumber,
            StControlNumber = 1,
            IsaSenderQualifier = "ZZ",
            SenderId = partner?.GsSenderId ?? "ERP-SENDER",
            ReceiverId = partner?.GsReceiverId ?? "ERP-RECEIVER",
            IsaReceiverQualifier = "ZZ",
            FunctionalId = "SH",
            GroupControlNumber = controlNumber.ToString(),
            Version = "005010",

            // 856-specific
            ShipmentId = shipmentNumber,
            ShipDate = shipmentDate,
            IsDelivered = true,

            // Parties
            SupplierName = "ERP Company",
            SupplierCode = "ERP",
            BuyerName = partner?.Name ?? "CUSTOMER",
            BuyerCode = partner?.PartnerCode ?? "CUST",
        };

        try
        {
            var ediContent = EdiX12Translator.Generate(doc);

            var transactionLog = new EdiTransactionLog(
                companyId,
                partner?.Id,
                partner?.PartnerCode ?? partnerCode ?? "UNKNOWN",
                "856",
                "Outbound",
                controlNumber.ToString(),
                controlNumber.ToString(),
                "1",
                ediContent.Length > 8000 ? ediContent[..8000] : ediContent);

            transactionLog.MarkProcessed(shipmentId);
            _dbContext.EdiTransactionLogs.Add(transactionLog);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "EDI 856 generated: Shipment={ShipmentNumber}, Partner={Partner}, {Length} chars",
                shipmentNumber, partner?.PartnerCode ?? "unknown", ediContent.Length);

            return OutboundEdiResult.Ok(ediContent, transactionLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate EDI 856 for shipment {ShipmentNumber}",
                shipmentNumber);

            return OutboundEdiResult.Fail(ex.Message);
        }
    }

    private async Task<TradingPartner?> FindPartnerAsync(
        Guid companyId,
        string? partnerCode,
        string documentType,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(partnerCode))
            return null;

        return await _dbContext.TradingPartners
            .Where(p => p.CompanyId == companyId
                && p.PartnerCode == partnerCode
                && p.IsActive)
            .FirstOrDefaultAsync(ct);
    }
}

public class OutboundEdiResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? EdiContent { get; set; }
    public Guid? TransactionLogId { get; set; }

    public static OutboundEdiResult Ok(string ediContent, Guid transactionLogId) =>
        new() { IsSuccess = true, EdiContent = ediContent, TransactionLogId = transactionLogId };

    public static OutboundEdiResult Fail(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
