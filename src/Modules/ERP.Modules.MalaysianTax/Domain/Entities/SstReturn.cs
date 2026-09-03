// <copyright file="SstReturn.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.MalaysianTax.Domain.Entities;

/// <summary>
/// Represents a bi-monthly SST return (SST-02 for Sales Tax, SST-02A for Service Tax).
/// Covers a two-month filing period aligned with LHDN deadlines.
/// </summary>
public class SstReturn
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the company scoping identifier.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the filing period start (1st of odd month: Jan, Mar, May, Jul, Sep, Nov).</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Gets or sets the filing period end (last day of the even month following PeriodStart).</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>Gets or sets the return type: SST-02 (Sales Tax) or SST-02A (Service Tax).</summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>Gets or sets the return status: Draft, Prepared, Filed, Amended.</summary>
    public string Status { get; set; } = "Draft";

    // ---- Computed Totals ----

    /// <summary>Gets or sets the total taxable amount for the period.</summary>
    public decimal TotalTaxableAmount { get; set; }

    /// <summary>Gets or sets the total tax collected/payable.</summary>
    public decimal TotalTaxCollected { get; set; }

    /// <summary>Gets or sets the total tax paid (for SST-02 credit notes / adjustments).</summary>
    public decimal TotalTaxPaid { get; set; }

    /// <summary>Gets the net tax payable (collected - paid).</summary>
    public decimal NetTaxPayable => TotalTaxCollected - TotalTaxPaid;

    /// <summary>Gets or sets the total taxable amount of exempt supplies (for SST-02 disclosure).</summary>
    public decimal ExemptAmount { get; set; }

    /// <summary>Gets or sets the summary JSON — line items by tax code / tax rate breakdown.</summary>
    public string? LineItemsJson { get; set; }

    // ---- Metadata ----

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the filed-at timestamp (ISO 8601).</summary>
    public string? FiledAt { get; set; }

    /// <summary>Gets or sets the LHDN reference number.</summary>
    public string? LhdnReference { get; set; }

    // ---- Navigation ----

    /// <summary>Gets or sets the related SST registration.</summary>
    public CompanySstRegistration? SstRegistration { get; set; }
}
