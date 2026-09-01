// <copyright file="CompanyDocumentHeader.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Shared.Kernel.Documents;

/// <summary>
/// Company header fields common to every customer-facing document (invoice,
/// pick list, packing slip, shipping list). Populated by the consuming module
/// from the Platform company + AR customer masters and handed to the QuestPDF
/// renderers in each module so all four documents share an identical header.
/// </summary>
public sealed record CompanyDocumentHeader(
    string CompanyName,
    string? CompanyLegalName,
    string? CompanyTaxId,
    string? CompanyAddress,
    string CompanyCurrency,
    string? CustomerName,
    string? CustomerTaxId,
    string? CustomerAddress,
    string DocumentTitle,
    string DocumentNumber,
    DateTimeOffset? DocumentDate);
