// <copyright file="PickListDocument.cs" company="ERP Project">
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
/// Pick list — warehouse-facing document listing every line on a sales order
/// that still needs to be picked (description, ordered qty, remaining to pick).
/// No pricing is shown. Reuses the <see cref="PickList"/>/<see cref="PickListLine"/>
/// DTO already returned by <see cref="FulfillmentDocumentsController"/>.
/// </summary>
internal sealed class PickListDocument(
    CompanyDocumentHeader header,
    PickList pickList,
    string customerName) : IDocument
{
    private readonly CompanyDocumentHeader _header = header;
    private readonly PickList _pickList = pickList;
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
            col.Item().Text("Pick List").SemiBold().FontSize(20);
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(26); // #
                    columns.RelativeColumn(3);  // description
                    columns.ConstantColumn(50); // uom
                    columns.ConstantColumn(50); // ordered
                    columns.ConstantColumn(55); // to pick
                });

                t.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("#").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).Text("Item").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("UoM").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Ordered").SemiBold().FontSize(8);
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("To Pick").SemiBold().FontSize(8);
                });

                var lines = _pickList.Lines.ToList();
                for (var i = 0; i < lines.Count; i++)
                {
                    var l = lines[i];
                    t.Cell().Element(BodyCellStyle).AlignCenter().Text((i + 1).ToString(CultureInfo.InvariantCulture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).Text(l.Description).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignCenter().Text(l.UnitOfMeasure).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignRight().Text(l.Quantity.ToString("N2", CultureInfo.InvariantCulture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignRight().Text(l.RemainingToPick.ToString("N2", CultureInfo.InvariantCulture)).Bold().FontSize(8);
                }

                static IContainer HeaderCellStyle(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(2).Background(Colors.Grey.Lighten3);

                static IContainer BodyCellStyle(IContainer c) => c.PaddingVertical(3).PaddingHorizontal(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            });

            col.Item().PaddingTop(12).Text($"Prepared for warehouse picking. Bill-to: {_customerName}").FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }
}
