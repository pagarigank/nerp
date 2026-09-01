// <copyright file="PackingSlipDocument.cs" company="ERP Project">
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
/// Packing slip — customer-facing document listing the items shipped on a
/// shipment (description, qty, UoM) with no pricing detail. Reuses the
/// <see cref="PackingSlip"/>/<see cref="PackingSlipLine"/> DTOs.
/// </summary>
internal sealed class PackingSlipDocument(
    CompanyDocumentHeader header,
    PackingSlip packingSlip,
    string customerName) : IDocument
{
    private readonly CompanyDocumentHeader _header = header;
    private readonly PackingSlip _packingSlip = packingSlip;
    private readonly string _customerName = customerName;

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
            col.Item().Text("Packing Slip").SemiBold().FontSize(20);
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            if (!string.IsNullOrWhiteSpace(_packingSlip.Carrier))
            {
                col.Item().Text($"Carrier: {_packingSlip.Carrier}").FontSize(9);
            }

            if (!string.IsNullOrWhiteSpace(_packingSlip.TrackingNumber))
            {
                col.Item().Text($"Tracking #: {_packingSlip.TrackingNumber}").FontSize(9);
            }

            col.Item().Text($"Ship Date: {_packingSlip.ShipmentDate:yyyy-MM-dd}").FontSize(9);
            col.Item().Text($"Customer: {_customerName}").FontSize(9);
            col.Item().Text($"Ship-to: {_header.CustomerAddress ?? _header.CompanyAddress}").FontSize(9);

            col.Item().PaddingTop(8).Table(t =>
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
                var lines = _packingSlip.Lines.ToList();
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

            col.Item().PaddingTop(14).Text("Thank you for your business!").FontSize(9).FontColor(Colors.Grey.Darken2);
        });
    }
}
