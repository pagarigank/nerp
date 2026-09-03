// <copyright file="EdiX12Translator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text;
using ERP.Modules.Integration.Domain.Entities;

namespace ERP.Modules.Integration.Infrastructure;

/// <summary>
/// X12 EDI translator that parses and generates EDI 850 (Purchase Order),
/// 810 (Invoice), and 856 (ASN/Shipment) documents per the X12 4010/5010
/// specification.
/// </summary>
public class EdiX12Translator
{
    private const char ElementSeparator = '*';
    private const char SegmentTerminator = '~';

    /// <summary>
    /// Parse an inbound X12 EDI document.
    /// </summary>
    /// <param name="ediContent">Raw EDI content.</param>
    /// <param name="documentType">Expected document type (850, 810, 856).</param>
    /// <returns>Parsed result with envelope and document-specific DTO.</returns>
    public static EdiParseResult Parse(string ediContent, string documentType)
    {
        if (string.IsNullOrWhiteSpace(ediContent))
            return EdiParseResult.Fail("Empty EDI content");

        var lines = ediContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3)
            return EdiParseResult.Fail("EDI document too short — missing envelope segments");

        try
        {
            var isaSegments = lines[0].TrimEnd(SegmentTerminator).Split(ElementSeparator);
            if (isaSegments.Length < 16)
                return EdiParseResult.Fail("ISA segment has fewer than 16 elements");

            var envelope = new EdiEnvelope
            {
                IsaControlNumber = isaSegments[13].Trim(),
                IsaSenderQualifier = isaSegments[5].Trim(),
                IsaSenderId = isaSegments[6].Trim(),
                IsaReceiverQualifier = isaSegments[7].Trim(),
                IsaReceiverId = isaSegments[8].Trim(),
                IsaDate = ParseEdiDate(isaSegments[9].Trim(), isaSegments[10].Trim()),
                IsaVersion = isaSegments[12].Trim(),
                AcknowledgmentRequested = isaSegments[14].Trim() == "1",
                UsageIndicator = isaSegments[15].Trim(),
            };

            var gsSegments = lines[1].TrimEnd(SegmentTerminator).Split(ElementSeparator);
            if (gsSegments.Length < 8)
                return EdiParseResult.Fail("GS segment has fewer than 8 elements");

            envelope.GsControlNumber = gsSegments[6].Trim();
            envelope.GsSenderId = gsSegments[2].Trim();
            envelope.GsReceiverId = gsSegments[3].Trim();
            envelope.GsFunctionalId = gsSegments[1].Trim();
            envelope.GsDate = ParseEdiDateTime(gsSegments[4].Trim(), gsSegments[5].Trim());

            var stSegments = lines[2].TrimEnd(SegmentTerminator).Split(ElementSeparator);
            if (stSegments.Length < 3)
                return EdiParseResult.Fail("ST segment has fewer than 3 elements");

            envelope.StControlNumber = stSegments[2].Trim();
            envelope.StDocumentType = stSegments[1].Trim();

            if (!string.Equals(envelope.StDocumentType, documentType, StringComparison.OrdinalIgnoreCase))
                return EdiParseResult.Fail($"ST document type '{envelope.StDocumentType}' does not match expected '{documentType}'");

            var bodySegments = new List<EdiSegment>();
            for (int i = 3; i < lines.Length - 1; i++)
            {
                var raw = lines[i].TrimEnd(SegmentTerminator);
                if (string.IsNullOrEmpty(raw)) continue;

                var segId = raw.Split(ElementSeparator)[0];
                if (segId == "SE") break;

                var elements = raw.Split(ElementSeparator).Skip(1).ToArray();
                bodySegments.Add(new EdiSegment { Id = segId, Elements = elements });
            }

            var seSegments = lines[^1].TrimEnd(SegmentTerminator).Split(ElementSeparator);
            if (seSegments.Length >= 2)
            {
                var declaredCount = int.Parse(seSegments[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                var actualCount = bodySegments.Count + 2;
                if (declaredCount != actualCount)
                    return EdiParseResult.Fail($"SE segment count mismatch: declared {declaredCount}, actual {actualCount}");
            }

            return documentType switch
            {
                "850" => ParsePurchaseOrder(envelope, bodySegments),
                "810" => ParseInvoice(envelope, bodySegments),
                "856" => ParseAsn(envelope, bodySegments),
                _ => EdiParseResult.Fail($"Unsupported document type: {documentType}"),
            };
        }
        catch (Exception ex)
        {
            return EdiParseResult.Fail($"Parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate an outbound X12 EDI document.
    /// </summary>
    /// <param name="doc">Document data to serialize.</param>
    /// <returns>Raw X12 EDI string.</returns>
    public static string Generate(EdiOutboundDocument doc)
    {
        var sb = new StringBuilder();
        var isaControlNumber = doc.IsaControlNumber.ToString().PadLeft(9, '0');
        var gsControlNumber = doc.GsControlNumber.ToString().PadLeft(5, '0');
        var stControlNumber = doc.StControlNumber.ToString().PadLeft(4, '0');
        var now = DateTime.UtcNow;

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"ISA*00*          *00*          *{doc.IsaSenderQualifier}*{doc.IsaSenderId.PadRight(15)}" +
                  $"*{doc.IsaReceiverQualifier}*{doc.ReceiverId.PadRight(15)}*" +
                  $"{now:yyMMdd}*{now:HHmm}*{doc.Version}*{isaControlNumber}" +
                  $"*{(doc.AcknowledgmentRequested ? "1" : "0")}*{doc.UsageIndicator}*{SegmentTerminator}\n");

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"GS*{doc.FunctionalId}*{doc.SenderId}*{doc.ReceiverId}*{now:yyyyMMdd}*{now:HHmm}" +
                  $"*{gsControlNumber}*{doc.GroupControlNumber}*{doc.Version}*{SegmentTerminator}\n");

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"ST*{doc.DocumentTypeCode}*{stControlNumber}*{doc.Version}{SegmentTerminator}\n");

        var bodySegments = doc.DocumentTypeCode switch
        {
            "850" => GeneratePurchaseOrderSegments(doc),
            "810" => GenerateInvoiceSegments(doc),
            "856" => GenerateAsnSegments(doc),
            _ => throw new NotSupportedException($"Document type {doc.DocumentTypeCode} not supported"),
        };

        foreach (var seg in bodySegments)
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"{seg}{SegmentTerminator}");

        var segmentCount = bodySegments.Count + 2;
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"SE*{segmentCount}*{stControlNumber}{SegmentTerminator}\n");
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"GE*1*{gsControlNumber}{SegmentTerminator}\n");
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"IEA*1*{isaControlNumber}{SegmentTerminator}\n");

        return sb.ToString();
    }

    private static EdiParseResult ParsePurchaseOrder(EdiEnvelope envelope, List<EdiSegment> segments)
    {
        var po = new EdiPurchaseOrder { Envelope = envelope };

        var begSeg = segments.FirstOrDefault(s => s.Id == "BEG");
        if (begSeg != null && begSeg.Elements.Length >= 5)
        {
            po.PurposeCode = begSeg.Elements[0];
            po.PurchaseOrderType = begSeg.Elements[1];
            po.PoNumber = begSeg.Elements[2];
            po.PoDate = begSeg.Elements.Length > 4 ? ParseEdiDate(begSeg.Elements[3], begSeg.Elements[4]) : DateTime.MinValue;
        }

        var n1Segments = segments.Where(s => s.Id == "N1").ToList();
        foreach (var n1 in n1Segments)
        {
            if (n1.Elements.Length < 2) continue;
            var qualifier = n1.Elements[0];
            var name = n1.Elements.Length > 1 ? n1.Elements[1] : string.Empty;
            var idQualifier = n1.Elements.Length > 2 ? n1.Elements[2] : string.Empty;
            var idCode = n1.Elements.Length > 3 ? n1.Elements[3] : string.Empty;

            var party = new EdiParty { Qualifier = qualifier, Name = name, IdQualifier = idQualifier, IdCode = idCode };

            if (qualifier is "SF" or "SU")
                po.Supplier = party;
            else if (qualifier is "ST" or "SH")
                po.ShipTo = party;
            else if (qualifier is "BT" or "OB")
                po.Buyer = party;
        }

        var po1Segments = segments.Where(s => s.Id == "PO1").ToList();
        int lineSeq = 0;
        foreach (var po1 in po1Segments)
        {
            if (po1.Elements.Length < 4) continue;
            lineSeq++;

            var line = new EdiPoLine
            {
                LineNumber = lineSeq,
                QuantityOrdered = decimal.TryParse(po1.Elements[0], out var qty) ? qty : 0,
                UnitOfMeasure = po1.Elements.Length > 1 ? po1.Elements[1] : "EA",
                UnitPrice = decimal.TryParse(po1.Elements.Length > 2 ? po1.Elements[2] : "0", out var price) ? price : 0,
                ProductIdQualifier = po1.Elements.Length > 3 ? po1.Elements[3] : string.Empty,
                ProductId = po1.Elements.Length > 4 ? po1.Elements[4] : string.Empty,
            };

            po.Lines.Add(line);
        }

        return EdiParseResult.Ok(po);
    }

    private static EdiParseResult ParseInvoice(EdiEnvelope envelope, List<EdiSegment> segments)
    {
        var invoice = new EdiInvoice { Envelope = envelope };

        var bigSeg = segments.FirstOrDefault(s => s.Id == "BIG");
        if (bigSeg != null && bigSeg.Elements.Length >= 4)
        {
            invoice.InvoiceDate = ParseEdiDate(bigSeg.Elements[0], bigSeg.Elements.Length > 1 ? bigSeg.Elements[1] : "0000");
            invoice.InvoiceNumber = bigSeg.Elements.Length > 2 ? bigSeg.Elements[2] : string.Empty;
            invoice.PoNumber = bigSeg.Elements.Length > 3 ? bigSeg.Elements[3] : string.Empty;
        }

        var it1Segments = segments.Where(s => s.Id == "IT1").ToList();
        int lineSeq = 0;
        foreach (var it1 in it1Segments)
        {
            if (it1.Elements.Length < 4) continue;
            lineSeq++;

            var line = new EdiInvoiceLine
            {
                LineNumber = lineSeq,
                QuantityInvoiced = decimal.TryParse(it1.Elements[0], out var qty) ? qty : 0,
                UnitOfMeasure = it1.Elements.Length > 1 ? it1.Elements[1] : "EA",
                UnitPrice = decimal.TryParse(it1.Elements.Length > 2 ? it1.Elements[2] : "0", out var price) ? price : 0,
                ProductIdQualifier = it1.Elements.Length > 3 ? it1.Elements[3] : string.Empty,
                ProductId = it1.Elements.Length > 4 ? it1.Elements[4] : string.Empty,
            };

            invoice.Lines.Add(line);
        }

        var tdsSeg = segments.FirstOrDefault(s => s.Id == "TDS");
        if (tdsSeg != null && tdsSeg.Elements.Length >= 1)
        {
            invoice.TotalAmount = decimal.TryParse(tdsSeg.Elements[0], out var total) ? total / 100m : 0;
        }

        return EdiParseResult.Ok(invoice);
    }

    private static EdiParseResult ParseAsn(EdiEnvelope envelope, List<EdiSegment> segments)
    {
        var asn = new EdiAsn { Envelope = envelope };

        var bsnSeg = segments.FirstOrDefault(s => s.Id == "BSN");
        if (bsnSeg != null && bsnSeg.Elements.Length >= 4)
        {
            asn.ShipmentId = bsnSeg.Elements[1];
            asn.ShipmentDate = ParseEdiDate(bsnSeg.Elements[2], bsnSeg.Elements[3]);
            asn.ShipmentType = bsnSeg.Elements.Length > 4 ? bsnSeg.Elements[4] : "0000";
        }

        var hlSegments = segments.Where(s => s.Id == "HL").ToList();
        EdiAsnHierLevel? currentShip = null;
        EdiAsnHierLevel? currentOrder = null;

        foreach (var hl in hlSegments)
        {
            if (hl.Elements.Length < 2) continue;

            var hlId = hl.Elements[0];
            var parentId = hl.Elements.Length > 1 ? hl.Elements[1] : string.Empty;
            var levelCode = hl.Elements.Length > 2 ? hl.Elements[2] : string.Empty;

            var level = new EdiAsnHierLevel
            {
                HlId = hlId,
                ParentId = parentId,
                LevelCode = levelCode,
            };

            var prfIdx = segments.IndexOf(hl);
            if (prfIdx >= 0 && prfIdx + 1 < segments.Count && segments[prfIdx + 1].Id == "PRF")
            {
                var prf = segments[prfIdx + 1];
                level.PoNumber = prf.Elements.Length > 0 ? prf.Elements[0] : string.Empty;
            }

            if (prfIdx >= 0 && prfIdx + 1 < segments.Count && segments[prfIdx + 1].Id == "MAN")
            {
                var man = segments[prfIdx + 1];
                level.TrackingNumber = man.Elements.Length > 1 ? man.Elements[1] : string.Empty;
            }

            if (levelCode == "S") { currentShip = level; asn.ShipmentLevels.Add(level); }
            else if (levelCode == "O" && currentShip != null) { currentOrder = level; currentShip.OrderLevels.Add(level); }
            else if (levelCode == "P" && currentOrder != null) { currentOrder.ItemLevels.Add(level); }
        }

        return EdiParseResult.Ok(asn);
    }

    private static List<string> GeneratePurchaseOrderSegments(EdiOutboundDocument doc)
    {
        var segments = new List<string>();
        segments.Add($"BEG*{doc.PurposeCode}*{doc.PurchaseOrderType}*{doc.PoNumber}*{doc.PoDate:yyyyMMdd}");

        if (!string.IsNullOrEmpty(doc.SupplierName))
            segments.Add($"N1*SF*{doc.SupplierName}*92*{doc.SupplierCode}");
        if (!string.IsNullOrEmpty(doc.ShipToName))
            segments.Add($"N1*ST*{doc.ShipToName}*92*{doc.ShipToCode}");

        int lineNum = 0;
        foreach (var line in doc.Lines)
        {
            lineNum++;
            segments.Add($"PO1*{line.Quantity:F0}*{line.UnitOfMeasure}*{line.UnitPrice:F4}*{line.ProductIdQualifier}*{line.ProductId}");
        }

        segments.Add($"CTT*{lineNum}");
        return segments;
    }

    private static List<string> GenerateInvoiceSegments(EdiOutboundDocument doc)
    {
        var segments = new List<string>();
        segments.Add($"BIG*{doc.InvoiceDate:yyyyMMdd}*{doc.InvoiceNumber}*{doc.PoDate:yyyyMMdd}*{doc.PoNumber}");

        if (!string.IsNullOrEmpty(doc.SupplierName))
            segments.Add($"N1*SE*{doc.SupplierName}*92*{doc.SupplierCode}");
        if (!string.IsNullOrEmpty(doc.BuyerName))
            segments.Add($"N1*RI*{doc.BuyerName}*92*{doc.BuyerCode}");

        int lineNum = 0;
        decimal totalAmount = 0;
        foreach (var line in doc.Lines)
        {
            lineNum++;
            var lineAmount = line.Quantity * line.UnitPrice;
            totalAmount += lineAmount;
            segments.Add($"IT1*{lineNum}*{line.Quantity:F0}*{line.UnitOfMeasure}*{line.UnitPrice:F4}*{line.ProductIdQualifier}*{line.ProductId}");
        }

        segments.Add($"TDS*{(int)(totalAmount * 100)}");
        segments.Add($"CTT*{lineNum}");
        return segments;
    }

    private static List<string> GenerateAsnSegments(EdiOutboundDocument doc)
    {
        var segments = new List<string>();
        segments.Add($"BSN*00*{doc.ShipmentId}*{doc.ShipDate:yyyyMMdd}*{doc.ShipDate:HHmm}*{(doc.IsDelivered ? "0002" : "0000")}");
        segments.Add($"DTM*011*{doc.ShipDate:yyyyMMdd}*{doc.ShipDate:HHmm}");
        segments.Add("HL*1**S");
        segments.Add("HL*2*1*O");
        segments.Add($"PRF*{doc.PoNumber}");

        int itemHlId = 3;
        int itemSeq = 0;
        foreach (var line in doc.Lines)
        {
            itemSeq++;
            segments.Add($"HL*{itemHlId}*2*P");
            segments.Add($"SN1*{itemSeq}*{line.Quantity:F0}*{line.UnitOfMeasure}");
            segments.Add($"LIN*{itemSeq}*{line.ProductIdQualifier}*{line.ProductId}");

            if (!string.IsNullOrEmpty(line.TrackingNumber))
                segments.Add($"MAN*GM*{line.TrackingNumber}");

            itemHlId++;
        }

        segments.Add($"CTT*{itemHlId - 1}");
        return segments;
    }

    private static DateTime ParseEdiDate(string dateStr, string timeStr)
    {
        if (dateStr.Length == 6 && int.TryParse(dateStr[..2], out var yy))
        {
            var year = yy > 50 ? 1900 + yy : 2000 + yy;
            if (int.TryParse(dateStr[2..4], out var mm) && int.TryParse(dateStr[4..6], out var dd))
            {
                var hour = 0;
                var min = 0;
                if (timeStr.Length >= 4)
                {
                    int.TryParse(timeStr[..2], out hour);
                    int.TryParse(timeStr[2..4], out min);
                }

                return new DateTime(year, mm, dd, hour, min, 0, DateTimeKind.Utc);
            }
        }

        return DateTime.MinValue;
    }

    private static DateTime ParseEdiDateTime(string dateStr, string timeStr) => ParseEdiDate(dateStr, timeStr);
}

public class EdiEnvelope
{
    public string IsaControlNumber { get; set; } = string.Empty;
    public string IsaSenderQualifier { get; set; } = string.Empty;
    public string IsaSenderId { get; set; } = string.Empty;
    public string IsaReceiverQualifier { get; set; } = string.Empty;
    public string IsaReceiverId { get; set; } = string.Empty;
    public DateTime IsaDate { get; set; }
    public string IsaVersion { get; set; } = string.Empty;
    public bool AcknowledgmentRequested { get; set; }
    public string UsageIndicator { get; set; } = "P";
    public string GsControlNumber { get; set; } = string.Empty;
    public string GsSenderId { get; set; } = string.Empty;
    public string GsReceiverId { get; set; } = string.Empty;
    public string GsFunctionalId { get; set; } = string.Empty;
    public DateTime GsDate { get; set; }
    public string StControlNumber { get; set; } = string.Empty;
    public string StDocumentType { get; set; } = string.Empty;
}

public class EdiSegment
{
    public string Id { get; set; } = string.Empty;
    public string[] Elements { get; set; } = Array.Empty<string>();
}

public class EdiParseResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public object? Document { get; private set; }

    public static EdiParseResult Ok(object doc) => new() { IsSuccess = true, Document = doc };
    public static EdiParseResult Fail(string error) => new() { IsSuccess = false, ErrorMessage = error };

    public T? As<T>() => Document is T t ? t : default;
}

public class EdiParty
{
    public string Qualifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IdQualifier { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
}

public class EdiPurchaseOrder
{
    public EdiEnvelope Envelope { get; set; } = new();
    public string PurposeCode { get; set; } = string.Empty;
    public string PurchaseOrderType { get; set; } = string.Empty;
    public string PoNumber { get; set; } = string.Empty;
    public DateTime PoDate { get; set; }
    public EdiParty? Supplier { get; set; }
    public EdiParty? ShipTo { get; set; }
    public EdiParty? Buyer { get; set; }
    public List<EdiPoLine> Lines { get; set; } = new();
}

public class EdiPoLine
{
    public int LineNumber { get; set; }
    public decimal QuantityOrdered { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public string ProductIdQualifier { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
}

public class EdiInvoice
{
    public EdiEnvelope Envelope { get; set; } = new();
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<EdiInvoiceLine> Lines { get; set; } = new();
}

public class EdiInvoiceLine
{
    public int LineNumber { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public string ProductIdQualifier { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
}

public class EdiAsn
{
    public EdiEnvelope Envelope { get; set; } = new();
    public string ShipmentId { get; set; } = string.Empty;
    public DateTime ShipmentDate { get; set; }
    public string ShipmentType { get; set; } = string.Empty;
    public List<EdiAsnHierLevel> ShipmentLevels { get; set; } = new();
}

public class EdiAsnHierLevel
{
    public string HlId { get; set; } = string.Empty;
    public string ParentId { get; set; } = string.Empty;
    public string LevelCode { get; set; } = string.Empty;
    public string PoNumber { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public List<EdiAsnHierLevel> OrderLevels { get; set; } = new();
    public List<EdiAsnHierLevel> ItemLevels { get; set; } = new();
}

public class EdiOutboundDocument
{
    public int IsaControlNumber { get; set; }
    public int GsControlNumber { get; set; }
    public int StControlNumber { get; set; }
    public string IsaSenderQualifier { get; set; } = "ZZ";
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string IsaSenderId => SenderId;
    public string IsaReceiverQualifier { get; set; } = "ZZ";
    public bool AcknowledgmentRequested { get; set; } = true;
    public string UsageIndicator { get; set; } = "P";
    public string FunctionalId { get; set; } = string.Empty;
    public string GroupControlNumber { get; set; } = string.Empty;
    public string Version { get; set; } = "005010";
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string PurposeCode { get; set; } = "00";
    public string PurchaseOrderType { get; set; } = "SA";
    public string PoNumber { get; set; } = string.Empty;
    public DateTime PoDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string ShipmentId { get; set; } = string.Empty;
    public DateTime ShipDate { get; set; }
    public bool IsDelivered { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string ShipToName { get; set; } = string.Empty;
    public string ShipToCode { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerCode { get; set; } = string.Empty;
    public List<EdiOutboundLine> Lines { get; set; } = new();
}

public class EdiOutboundLine
{
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public string ProductIdQualifier { get; set; } = "IN";
    public string ProductId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
}
