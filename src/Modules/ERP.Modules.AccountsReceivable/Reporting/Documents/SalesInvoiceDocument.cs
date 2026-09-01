// <copyright file="SalesInvoiceDocument.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using ERP.Shared.Kernel.Documents;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Modules.AccountsReceivable.Reporting.Documents;

/// <summary>
/// Sales invoice — customer-facing document with company header, bill-to /
/// ship-to, line items (description, qty, unit price, tax, line total),
/// subtotal / tax / discount / total and balance due.
/// </summary>
internal sealed class SalesInvoiceDocument : IDocument
{
    private readonly CompanyDocumentHeader _header;
    private readonly SalesInvoiceView _view;

    public SalesInvoiceDocument(CompanyDocumentHeader header, SalesInvoiceView view)
    {
        _header = header;
        _view = view;
    }

    private static IContainer LabelStyle(IContainer c) => c.PaddingVertical(2).PaddingHorizontal(2);

    private static IContainer ValueStyle(IContainer c) => c.PaddingVertical(2).PaddingHorizontal(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

    private static IContainer TotalLabelStyle(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(2);

    private static IContainer TotalValueStyle(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(2);

    private static IContainer HeaderCellStyle(IContainer c) => c.PaddingVertical(4).PaddingHorizontal(2).Background(Colors.Grey.Lighten3);

    private static IContainer BodyCellStyle(IContainer c) => c.PaddingVertical(3).PaddingHorizontal(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

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
        var culture = CultureInfo.InvariantCulture;

        container.Column(col =>
        {
            col.Item().Text($"Invoice #{_view.InvoiceNumber}").SemiBold().FontSize(22);
            col.Item().Text($"Invoice Date: {_view.InvoiceDate:yyyy-MM-dd}").FontSize(9);
            col.Item().Text($"Due Date: {_view.DueDate:yyyy-MM-dd}").FontSize(9);

            if (!string.IsNullOrWhiteSpace(_view.CustomerPurchaseOrder))
            {
                col.Item().Text($"PO Number: {_view.CustomerPurchaseOrder}").FontSize(9);
            }

            if (!string.IsNullOrWhiteSpace(_view.PaymentTerms))
            {
                col.Item().Text($"Terms: {_view.PaymentTerms}").FontSize(9);
            }

            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(60);
                });

                t.Header(headerCell =>
                {
                    headerCell.Cell().Element(HeaderCellStyle).Text("Item / Description").SemiBold().FontSize(8);
                    headerCell.Cell().Element(HeaderCellStyle).AlignCenter().Text("Qty").SemiBold().FontSize(8);
                    headerCell.Cell().Element(HeaderCellStyle).AlignRight().Text("Unit Price").SemiBold().FontSize(8);
                    headerCell.Cell().Element(HeaderCellStyle).AlignRight().Text("Tax").SemiBold().FontSize(8);
                    headerCell.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount").SemiBold().FontSize(8);
                });

                foreach (var l in _view.Lines)
                {
                    t.Cell().Element(BodyCellStyle).Text(l.Description).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignCenter().Text(l.Quantity.ToString("N2", culture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignRight().Text(l.UnitPrice.ToString("N2", culture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignRight().Text(l.TaxAmount.ToString("N2", culture)).FontSize(8);
                    t.Cell().Element(BodyCellStyle).AlignRight().Text(l.TotalAmount.ToString("N2", culture)).FontSize(8);
                }
            });

            col.Item().PaddingTop(8).AlignRight().Table(t =>
            {
                t.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(70);
                });

                t.Cell().Element(LabelStyle).AlignRight().Text("Subtotal:").FontSize(8);
                t.Cell().Element(ValueStyle).AlignRight().Text(_view.Subtotal.ToString("N2", culture)).FontSize(8);
                t.Cell().Element(LabelStyle).AlignRight().Text("Tax:").FontSize(8);
                t.Cell().Element(ValueStyle).AlignRight().Text(_view.TaxTotal.ToString("N2", culture)).FontSize(8);
                t.Cell().Element(LabelStyle).AlignRight().Text("Discount:").FontSize(8);
                t.Cell().Element(ValueStyle).AlignRight().Text(_view.DiscountTotal.ToString("N2", culture)).FontSize(8);
                t.Cell().Element(LabelStyle).AlignRight().Text("Shipping:").FontSize(8);
                t.Cell().Element(ValueStyle).AlignRight().Text(_view.ShippingAmount.ToString("N2", culture)).FontSize(8);
                t.Cell().Element(TotalLabelStyle).AlignRight().Text("TOTAL:").SemiBold().FontSize(10);
                t.Cell().Element(TotalValueStyle).AlignRight().Text(_view.TotalAmount.ToString("N2", culture)).SemiBold().FontSize(10);
            });

            col.Item().PaddingTop(6).AlignRight().Text($"Balance Due: {_view.BalanceDue.ToString("N2", culture)}").FontSize(9).SemiBold();
        });
    }
}

public sealed record SalesInvoiceView(
    Guid InvoiceId,
    Guid CompanyId,
    Guid CustomerId,
    string InvoiceNumber,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    string? CustomerPurchaseOrder,
    string? PaymentTerms,
    decimal Subtotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal ShippingAmount,
    decimal TotalAmount,
    decimal BalanceDue,
    IReadOnlyList<SalesInvoiceLineView> Lines);

public sealed record SalesInvoiceLineView(
    Guid AccountId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount);