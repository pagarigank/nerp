// <copyright file="InboundEdiProcessor.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.AccountsReceivable.Domain.Entities;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.Integration.Infrastructure;

/// <summary>
/// Processes inbound EDI documents (850, 810, 856) by parsing them with
/// EdiX12Translator and creating the corresponding ERP entities in the
/// appropriate module DbContexts.
/// </summary>
public class InboundEdiProcessor
{
    private readonly EdiX12Translator _translator;
    private readonly IntegrationDbContext _integrationDb;
    private readonly PurchasingDbContext _purchasingDb;
    private readonly ArDbContext _arDb;
    private readonly PlatformDbContext _platformDb;
    private readonly ILogger<InboundEdiProcessor> _logger;

    public InboundEdiProcessor(
        EdiX12Translator translator,
        IntegrationDbContext integrationDb,
        PurchasingDbContext purchasingDb,
        ArDbContext arDb,
        PlatformDbContext platformDb,
        ILogger<InboundEdiProcessor> logger)
    {
        _translator = translator;
        _integrationDb = integrationDb;
        _purchasingDb = purchasingDb;
        _arDb = arDb;
        _platformDb = platformDb;
        _logger = logger;
    }

    /// <summary>
    /// Process an inbound EDI document. Parses the content, creates the
    /// corresponding ERP entity, and logs the transaction.
    /// </summary>
    /// <param name="companyId">Company receiving the EDI.</param>
    /// <param name="ediContent">Raw EDI X12 content.</param>
    /// <param name="documentType">Expected document type (850, 810, 856).</param>
    /// <param name="partnerCode">Trading partner code (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Processing result with entity reference.</returns>
    public async Task<InboundEdiResult> ProcessAsync(
        Guid companyId,
        string ediContent,
        string documentType,
        string? partnerCode = null,
        CancellationToken ct = default)
    {
        // 1. Parse EDI
        var parseResult = EdiX12Translator.Parse(ediContent, documentType);

        // 2. Log transaction
        var transactionLog = new EdiTransactionLog(
            companyId,
            tradingPartnerId: null,
            partnerCode: partnerCode ?? "UNKNOWN",
            documentType: documentType,
            direction: "Inbound",
            isaControlNumber: null,
            gsControlNumber: null,
            stControlNumber: null,
            rawContent: ediContent.Length > 8000 ? ediContent[..8000] : ediContent);

        if (!parseResult.IsSuccess)
        {
            transactionLog.MarkFailed(parseResult.ErrorMessage ?? "Parse failed");
            _integrationDb.EdiTransactionLogs.Add(transactionLog);
            await _integrationDb.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Inbound EDI parse failed: {DocType}, Error: {Error}",
                documentType, parseResult.ErrorMessage);

            return InboundEdiResult.Fail(parseResult.ErrorMessage ?? "Parse failed");
        }

        // 3. Route to appropriate module
        try
        {
            var entityId = documentType switch
            {
                "850" => await ProcessInbound850Async(companyId, parseResult, ct),
                "810" => await ProcessInbound810Async(companyId, parseResult, ct),
                "856" => await ProcessInbound856Async(companyId, parseResult, ct),
                _ => throw new NotSupportedException($"Document type {documentType} not supported for inbound processing"),
            };

            transactionLog.MarkProcessed(entityId);
            _integrationDb.EdiTransactionLogs.Add(transactionLog);
            await _integrationDb.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Inbound EDI processed: {DocType} from {Partner}, EntityId={EntityId}",
                documentType, partnerCode, entityId);

            return InboundEdiResult.Ok(entityId, documentType);
        }
        catch (Exception ex)
        {
            transactionLog.MarkFailed(ex.Message);
            _integrationDb.EdiTransactionLogs.Add(transactionLog);
            await _integrationDb.SaveChangesAsync(ct);

            _logger.LogError(ex,
                "Inbound EDI processing failed: {DocType} from {Partner}",
                documentType, partnerCode);

            return InboundEdiResult.Fail(ex.Message);
        }
    }

    private async Task<Guid> ProcessInbound850Async(
        Guid companyId,
        EdiParseResult parseResult,
        CancellationToken ct)
    {
        var ediPo = parseResult.As<EdiPurchaseOrder>()
            ?? throw new InvalidOperationException("Failed to cast EDI result to PurchaseOrder");

        // Find or create vendor by partner code
        var vendorId = await FindOrCreateVendorAsync(companyId, ediPo.Supplier, ct);

        var po = new PurchaseOrder(
            poNumber: ediPo.PoNumber,
            companyId: companyId,
            vendorId: vendorId,
            orderDate: ediPo.PoDate != DateTime.MinValue ? ediPo.PoDate : DateTime.UtcNow,
            orderType: ediPo.PurposeCode == "02" ? PurchaseOrderType.Blanket : PurchaseOrderType.Standard,
            shipToName: ediPo.ShipTo?.Name,
            shipToAddress: null,
            paymentTermId: null,
            buyerId: null,
            buyerNotes: $"EDI 850 from partner",
            vendorReference: ediPo.PoNumber);

        foreach (var ediLine in ediPo.Lines)
        {
            var line = new PurchaseOrderLine(
                purchaseOrderId: po.Id,
                lineNumber: ediLine.LineNumber,
                itemId: ediLine.ProductId,
                description: $"EDI line {ediLine.LineNumber}: {ediLine.ProductId}",
                quantity: ediLine.QuantityOrdered,
                unitOfMeasure: ediLine.UnitOfMeasure,
                unitPrice: ediLine.UnitPrice,
                needByDate: ediPo.PoDate != DateTime.MinValue ? ediPo.PoDate : null,
                accountId: null,
                projectId: null,
                taskId: null);

            po.AddLine(line);
        }

        _purchasingDb.PurchaseOrders.Add(po);
        await _purchasingDb.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created PO from EDI 850: PO# {PoNumber}, {LineCount} lines, Vendor={VendorId}",
            po.PONumber, ediPo.Lines.Count, vendorId);

        return po.Id;
    }

    private async Task<Guid> ProcessInbound810Async(
        Guid companyId,
        EdiParseResult parseResult,
        CancellationToken ct)
    {
        var ediInvoice = parseResult.As<EdiInvoice>()
            ?? throw new InvalidOperationException("Failed to cast EDI result to Invoice");

        // Find or create customer by partner code
        var customerId = await FindOrCreateCustomerAsync(companyId, ediInvoice.Envelope?.IsaSenderId, ct);

        // Find the current fiscal period
        var fiscalPeriodId = await _platformDb.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.Status == PeriodStatus.Open)
            .OrderBy(p => p.PeriodNumber)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (fiscalPeriodId == Guid.Empty)
            throw new InvalidOperationException($"No open fiscal period found for company {companyId}");

        var batch = new InvoiceBatch(
            companyId: companyId,
            batchNumber: $"EDI-810-{ediInvoice.InvoiceNumber}",
            description: $"EDI 810 Invoice from {ediInvoice.Envelope?.IsaSenderId}",
            postingDate: ediInvoice.InvoiceDate != DateTime.MinValue ? ediInvoice.InvoiceDate : DateTimeOffset.UtcNow,
            fiscalPeriodId: fiscalPeriodId);

        var invoice = batch.AddInvoice(
            customerId: customerId,
            invoiceNumber: ediInvoice.InvoiceNumber,
            invoiceDate: ediInvoice.InvoiceDate != DateTime.MinValue ? ediInvoice.InvoiceDate : DateTimeOffset.UtcNow,
            dueDate: (ediInvoice.InvoiceDate != DateTime.MinValue ? ediInvoice.InvoiceDate : DateTimeOffset.UtcNow).AddDays(30),
            description: $"EDI 810 Invoice #{ediInvoice.InvoiceNumber}",
            paymentTermId: null,
            projectId: null,
            salesOrderId: null);

        foreach (var ediLine in ediInvoice.Lines)
        {
            var lineAmount = ediLine.QuantityInvoiced * ediLine.UnitPrice;
            invoice.AddLine(
                accountId: Guid.Empty, // TODO: Map EDI product to GL account
                description: $"EDI line {ediLine.LineNumber}: {ediLine.ProductId}",
                quantity: ediLine.QuantityInvoiced,
                unitPrice: ediLine.UnitPrice,
                taxAmount: 0, // TODO: Apply tax determination rules
                discountAmount: null);
        }

        _arDb.InvoiceBatches.Add(batch);
        await _arDb.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created AR Invoice batch from EDI 810: Batch# {BatchNumber}, Invoice# {InvoiceNumber}",
            batch.BatchNumber, ediInvoice.InvoiceNumber);

        return batch.Id;
    }

    private Task<Guid> ProcessInbound856Async(
        Guid companyId,
        EdiParseResult parseResult,
        CancellationToken ct)
    {
        var ediAsn = parseResult.As<EdiAsn>()
            ?? throw new InvalidOperationException("Failed to cast EDI result to ASN");

        // For 856 ASN, we log the shipment reference but don't create a full
        // Shipment entity (that's the OrderManagement module's responsibility).
        // Instead, we store the ASN details in the transaction log for reference.
        // The actual shipment confirmation flow (856 -> Shipment status update)
        // requires integration with the OM module's ShipmentController.

        _logger.LogInformation(
            "Received EDI 856 ASN: ShipmentId={ShipmentId}, PO={PoNumber}, Items={ItemCount}",
            ediAsn.ShipmentId,
            ediAsn.ShipmentLevels.FirstOrDefault()?.OrderLevels.FirstOrDefault()?.PoNumber,
            ediAsn.ShipmentLevels.SelectMany(s => s.OrderLevels).SelectMany(o => o.ItemLevels).Count());

        // Return the shipment ID as a reference (not a real entity ID in our system)
        return Task.FromResult(Guid.NewGuid());
    }

    private Task<Guid> FindOrCreateVendorAsync(
        Guid companyId,
        EdiParty? supplier,
        CancellationToken ct)
    {
        if (supplier == null)
            return Task.FromResult(Guid.Empty);

        // Try to find existing vendor by partner code
        var partnerCode = !string.IsNullOrEmpty(supplier.IdCode) ? supplier.IdCode : supplier.Name;

        // For now, return a placeholder vendor ID
        // In production, this would look up the vendor master or create one
        _logger.LogDebug(
            "EDI 850 supplier: {Name}, ID={IdCode}. Using placeholder vendor.",
            supplier.Name, supplier.IdCode);

        return Task.FromResult(Guid.Empty);
    }

    private Task<Guid> FindOrCreateCustomerAsync(
        Guid companyId,
        string? senderId,
        CancellationToken ct)
    {
        // Try to find existing customer by sender ID
        // For now, return a placeholder customer ID
        // In production, this would look up the customer master or create one
        _logger.LogDebug(
            "EDI 810 sender: {SenderId}. Using placeholder customer.",
            senderId);

        return Task.FromResult(Guid.Empty);
    }
}

public class InboundEdiResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }

    public static InboundEdiResult Ok(Guid entityId, string entityType) =>
        new() { IsSuccess = true, EntityId = entityId, EntityType = entityType };

    public static InboundEdiResult Fail(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
