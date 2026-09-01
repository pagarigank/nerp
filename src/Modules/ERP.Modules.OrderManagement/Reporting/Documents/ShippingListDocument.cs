// <copyright file="ShippingListDocument.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Modules.OrderManagement.Api;
using ERP.Shared.Kernel.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Modules.OrderManagement.Reporting.Documents;

/// <summary>
/// Shipping list / bill-of-lading manifest — warehouse-facing document: carrier,
/// tracking, ship date, destination and the line items being delivered. Reuses
/// the <see cref="PackingSlip"/>/<see cref="PackingSlipLine"/> DTOs.
/// </summary>
internal sealed class ShippingListDocument(
    CompanyDocumentHeader header,
    PackingSlip shipment,
    string customerName,
    int packageCount) : IDocument
{
    private readonly CompanyDocumentHeader _header = header;
    private readonly PackingSlip _shipment = shipment;
    private readonly string _customerName = customerName;
    private readonly int _packageCount = packageCount;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Element(x => DocumentHeader.Header(x, _header));
            page.Content().PaddingVertical(10).Element(BuildContent);
            page.Footer().Element(x => DocumentHeader.Footer(x));
        });
    }

    private void BuildContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Shipping List").SemiBold().FontSize(20);
            col.Item().Text($"Shipment #{_shipment.ShipmentNumber}").SemiBold().FontSize(12);
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            var rows = new (string Label, string Value)[]
            {
                ("Ship Date", _shipment.ShipmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("Customer", _customerName),
                ("Ship-to", _header.CustomerAddress ?? _header.CompanyAddress ?? string.Empty),
                ("Carrier", _shipment.Carrier ?? "(none)"),
                ("Tracking #", _shipment.TrackingNumber ?? "(none)"),
                ("Packages", _packageCount.ToString(CultureInfo.InvariantCulture)),
                ("Sales Order", _shipment.SalesOrderId?.ToString() ?? "(none)"),
            };

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                foreach (var row in rows)
                {
                    t.Cell().Padding(2).Text($"{row.Label}:").FontSize(8);
                    t.Cell().Padding(2).Text(row.Value ?? string.Empty).FontSize(8);
                }
            });

            col.Item().PaddingTop(10).Table(t =>
            {
                t.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(26);  // #
                    columns.RelativeColumn(4);   // description
                    columns.ConstantColumn(50);  // qty
                    columns.ConstantColumn(50);  // uom
                });
                t.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("#").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Item").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Qty").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("UoM").SemiBold().FontSize(8);
                });
                var lines = _shipment.Lines.ToList();
                for (var i = 0; i < lines.Count; i++)
                {
                    var l = lines[i];
                    t.Cell().Element(BodyCellStyle).AlignCenter().Text((i + 1).ToString(CultureInfo.InvariantCulture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).Text(l.Description).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignCenter().Text(l.Quantity.ToString("N2", CultureInfo.InvariantCulture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignCenter().Text(l.UnitOfMeasure).FontSize(8);
                }

                static IContainer HeaderCellStyle(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(2).Background(Colors.Grey.Lighten3);

                static IContainer BodyCellStyle(IContainer c) => c.PaddingVertical(3).PaddingHorizontal(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            });

            col.Item().PaddingTop(12).Text($"Total line items shipped: {_shipment.Lines.Count}").FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }
}
