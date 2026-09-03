// <copyright file="CompanySstRegistration.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Modules.MalaysianTax.Domain.Entities;

/// <summary>
/// Company SST registration profile per RMCD requirements.
/// Tracks Sales Tax and Service Tax registration numbers, registration dates, and active status.
/// Each company can independently register for Sales Tax and/or Service Tax.
/// </summary>
[Table("company_sst_registration", Schema = "tax")]
public class CompanySstRegistration
{
    public CompanySstRegistration()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the company ID this registration belongs to.
    /// </summary>
    public Guid CompanyId { get; set; }

    // ---- Sales Tax Registration ----

    /// <summary>
    /// Gets or sets a value indicating whether the company is registered for Sales Tax with RMCD.
    /// </summary>
    public bool IsRegisteredForSalesTax { get; set; }

    /// <summary>
    /// Gets or sets the Sales Tax Registration Number issued by RMCD.
    /// Format: typically starts with a state code followed by digits.
    /// </summary>
    [MaxLength(50)]
    public string? SalesTaxRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the date the company registered for Sales Tax with RMCD.
    /// </summary>
    public DateTime? SalesTaxRegistrationDate { get; set; }

    /// <summary>
    /// Gets or sets the date the company was deregistered from Sales Tax (if applicable).
    /// Null means still registered.
    /// </summary>
    public DateTime? SalesTaxDeregistrationDate { get; set; }

    // ---- Service Tax Registration ----

    /// <summary>
    /// Gets or sets a value indicating whether the company is registered for Service Tax with RMCD.
    /// </summary>
    public bool IsRegisteredForServiceTax { get; set; }

    /// <summary>
    /// Gets or sets the Service Tax Registration Number issued by RMCD.
    /// </summary>
    [MaxLength(50)]
    public string? ServiceTaxRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the date the company registered for Service Tax with RMCD.
    /// </summary>
    public DateTime? ServiceTaxRegistrationDate { get; set; }

    /// <summary>
    /// Gets or sets the date the company was deregistered from Service Tax (if applicable).
    /// Null means still registered.
    /// </summary>
    public DateTime? ServiceTaxDeregistrationDate { get; set; }

    // ---- Tourism Tax (if applicable) ----

    /// <summary>
    /// Gets or sets a value indicating whether the company is registered for Tourism Tax.
    /// Only applicable to hotel/accommodation providers.
    /// </summary>
    public bool IsRegisteredForTourismTax { get; set; }

    /// <summary>
    /// Gets or sets the Tourism Tax Registration Number issued by RMCD.
    /// </summary>
    [MaxLength(50)]
    public string? TourismTaxRegistrationNumber { get; set; }

    // ---- General ----

    /// <summary>
    /// Gets or sets a value indicating whether this registration record is active.
    /// Soft-delete pattern for historical retention.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the general RMCD portal login reference (not the MyInvois Client ID).
    /// Useful for audit trail linkage.
    /// </summary>
    [MaxLength(100)]
    public string? RmcdPortalReference { get; set; }

    /// <summary>
    /// Gets or sets free-text notes about the registration (e.g., filing frequency, special categories).
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // ---- Timestamps ----

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ---- Business Methods ----

    /// <summary>
    /// Returns true if the company is registered for any SST type (Sales, Service, or Tourism).
    /// </summary>
    public bool IsRegisteredForAnySst()
    {
        return IsRegisteredForSalesTax || IsRegisteredForServiceTax || IsRegisteredForTourismTax;
    }

    /// <summary>
    /// Returns the effective Sales Tax registration number, or null/empty if not registered.
    /// </summary>
    public string? GetEffectiveSalesTaxNumber()
    {
        return IsRegisteredForSalesTax ? SalesTaxRegistrationNumber : null;
    }

    /// <summary>
    /// Returns the effective Service Tax registration number, or null/empty if not registered.
    /// </summary>
    public string? GetEffectiveServiceTaxNumber()
    {
        return IsRegisteredForServiceTax ? ServiceTaxRegistrationNumber : null;
    }

    /// <summary>
    /// Deregisters from Sales Tax with the given date.
    /// </summary>
    public void DeregisterSalesTax(DateTime deregistrationDate)
    {
        IsRegisteredForSalesTax = false;
        SalesTaxDeregistrationDate = deregistrationDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deregisters from Service Tax with the given date.
    /// </summary>
    public void DeregisterServiceTax(DateTime deregistrationDate)
    {
        IsRegisteredForServiceTax = false;
        ServiceTaxDeregistrationDate = deregistrationDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the full registration profile.
    /// </summary>
    public void UpdateProfile(
        bool isRegisteredForSalesTax,
        string? salesTaxRegNo,
        DateTime? salesTaxRegDate,
        bool isRegisteredForServiceTax,
        string? serviceTaxRegNo,
        DateTime? serviceTaxRegDate,
        bool isRegisteredForTourismTax,
        string? tourismTaxRegNo,
        string? rmcdPortalReference,
        string? notes)
    {
        IsRegisteredForSalesTax = isRegisteredForSalesTax;
        SalesTaxRegistrationNumber = salesTaxRegNo;
        SalesTaxRegistrationDate = salesTaxRegDate;

        IsRegisteredForServiceTax = isRegisteredForServiceTax;
        ServiceTaxRegistrationNumber = serviceTaxRegNo;
        ServiceTaxRegistrationDate = serviceTaxRegDate;

        IsRegisteredForTourismTax = isRegisteredForTourismTax;
        TourismTaxRegistrationNumber = tourismTaxRegNo;

        RmcdPortalReference = rmcdPortalReference;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
