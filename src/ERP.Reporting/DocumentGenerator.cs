// <copyright file="DocumentGenerator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERP.Reporting;

/// <summary>
/// Centralized QuestPDF license + generate helper. QuestPDF 2025.x requires an
/// explicit <see cref="LicenseType"/> to be configured before any document is
/// rendered (otherwise <c>ValidateLicense()</c> throws on <c>GeneratePdf</c>
/// with a licensing prompt). This wrapper guarantees the Community license is
/// applied and produces UTF-8 PDF bytes for any <see cref="IDocument"/>.
/// </summary>
public static class DocumentGenerator
{
    static DocumentGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Generates UTF-8 PDF bytes for a QuestPDF <see cref="IDocument"/>.
    /// </summary>
    /// <param name="document">The QuestPDF document to render.</param>
    /// <returns>A UTF-8 encoded byte array containing the rendered PDF.</returns>
    public static byte[] GeneratePdf(IDocument document) =>
        document.GeneratePdf();
}
