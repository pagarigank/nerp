// <copyright file="DocumentHeader.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Shared.Kernel.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Reporting;

/// <summary>
/// Shared QuestPDF header/footer reused by every document renderer across all modules.
/// Provides an identical company/customer header and a page footer (print timestamp + current/total page numbers).
/// </summary>
public static class DocumentHeader
{
    public static void Header(IContainer container, CompanyDocumentHeader header)
    {
        container.Row(row =>
        {
            // Left: company block
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(header.CompanyName).SemiBold().FontSize(18);

                if (!string.IsNullOrWhiteSpace(header.CompanyAddress))
                {
                    col.Item().Text(header.CompanyAddress).FontSize(8).FontColor(Colors.Grey.Darken2);
                }

                if (!string.IsNullOrWhiteSpace(header.CompanyTaxId))
                {
                    col.Item().Text($"Tax ID: {header.CompanyTaxId}").FontSize(8).FontColor(Colors.Grey.Darken2);
                }
            });

            // Right: document block
            row.ConstantItem(120).Column(col =>
            {
                col.Item().Text(header.DocumentTitle).SemiBold().FontSize(14);
                col.Item().Text(header.DocumentNumber).FontSize(10).Bold();

                if (header.DocumentDate.HasValue)
                {
                    col.Item().Text($"Date: {header.DocumentDate:yyyy-MM-dd}").FontSize(9);
                }
            });
        });
    }

    public static void Footer(IContainer container)
    {
        container.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2)).AlignCenter()
            .Text(text =>
            {
                text.Span($"Printed {DateTimeOffset.Now:yyyy-MM-dd HH:mm}  Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
    }
}
