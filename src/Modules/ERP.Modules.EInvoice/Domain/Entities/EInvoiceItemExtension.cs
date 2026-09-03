// <copyright file="EInvoiceItemExtension.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.Core.Domain.Common;

namespace ERP.Modules.EInvoice.Domain.Entities;

/// <summary>
/// e-Invoice extension for Item/Service master data.
/// Maps ERP items to MyInvois classification codes and unit of measure codes.
/// </summary>
[Table("einvoice_item_extension", Schema = "inv")]
public class EInvoiceItemExtension : AuditableAggregateRoot
{
    protected EInvoiceItemExtension() { }

    public EInvoiceItemExtension(
        Guid itemId,
        string? classificationCode = null,
        string? unitOfMeasureCode = null,
        string? productTariffCode = null,
        string? countryOfOrigin = null,
        bool isSubjectToEInvoice = true)
        : base(Guid.NewGuid())
    {
        ItemId = itemId;
        ClassificationCode = classificationCode;
        UnitOfMeasureCode = unitOfMeasureCode;
        ProductTariffCode = productTariffCode;
        CountryOfOrigin = countryOfOrigin;
        IsSubjectToEInvoice = isSubjectToEInvoice;
    }

    /// <summary>Gets the ERP Item ID this extension belongs to.</summary>
    public Guid ItemId { get; private set; }

    /// <summary>Gets the LHDN classification code (listID=CLASS).</summary>
    [MaxLength(20)]
    public string? ClassificationCode { get; private set; }

    /// <summary>Gets the UN/ECE Rec 20 unit of measure code (e.g., C62=Each, KGM=Kilogram).</summary>
    [MaxLength(10)]
    public string? UnitOfMeasureCode { get; private set; }

    /// <summary>Gets the product tariff code (HS code for goods).</summary>
    [MaxLength(20)]
    public string? ProductTariffCode { get; private set; }

    /// <summary>Gets the country of origin (ISO 3166-1 alpha-3).</summary>
    [MaxLength(3)]
    public string? CountryOfOrigin { get; private set; }

    /// <summary>Gets a value indicating whether this item is subject to e-Invoice.</summary>
    public bool IsSubjectToEInvoice { get; private set; }

    public void SetClassificationCode(string? code) => ClassificationCode = code;
    public void SetUnitOfMeasureCode(string? code) => UnitOfMeasureCode = code;
    public void SetProductTariffCode(string? code) => ProductTariffCode = code;
    public void SetCountryOfOrigin(string? country) => CountryOfOrigin = country;
    public void SetEInvoiceStatus(bool isSubject) => IsSubjectToEInvoice = isSubject;
}
