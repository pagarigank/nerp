// <copyright file="TaxFilingExportBuilder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text;

namespace ERP.Modules.Payroll.Application;

/// <summary>
/// Pure builders for payroll tax-filing export payloads (941, 940, W-2, W-3, state
/// quarterly). Each takes pre-aggregated rows and returns the file-content string
/// following the existing 1099 e-file convention: header line + CSV text rows.
/// The module tracks combined employee FICA; the statutory SS/Medicare split is
/// derived from the 6.2%/1.45% components of the combined 7.65% rate.
/// </summary>
/// <summary>Quarterly Form 941 aggregation for one employer.</summary>
public sealed record Form941Totals(
    int Year,
    int Quarter,
    decimal TotalWages,
    decimal IncomeTaxWithheld,
    decimal SocialSecurityWages,
    decimal SocialSecurityTax,
    decimal MedicareWages,
    decimal MedicareTax,
    decimal EmployerSocialSecurityTax,
    decimal EmployerMedicareTax);

/// <summary>Annual Form 940 (FUTA) aggregation.</summary>
public sealed record Form940Totals(
    int Year,
    decimal TotalWages,
    decimal FutaExemptWages,
    decimal FutaTaxableWages,
    decimal FutaRate,
    decimal FutaTax);

/// <summary>One employee's annual W-2 wage/tax figures.</summary>
public sealed record W2Row(
    string EmployeeCode,
    string EmployeeName,
    string SsnMasked,
    decimal Box1Wages,
    decimal Box2FederalIncomeTax,
    decimal Box3SocialSecurityWages,
    decimal Box4SocialSecurityTax,
    decimal Box5MedicareWages,
    decimal Box6MedicareTax,
    string StateCode,
    decimal StateWages);

/// <summary>Per-state quarterly wage aggregation.</summary>
public sealed record StateQuarterlyRow(string StateCode, decimal TotalWages, decimal ExcessWages, decimal Withholding);

public static class TaxFilingExportBuilder
{
    public const decimal SocialSecurityComponent = 0.062m;
    public const decimal MedicareComponent = 0.0145m;
    public const decimal CombinedFicaRate = SocialSecurityComponent + MedicareComponent;

    public static string BuildForm941(Form941Totals t, string ein, string employerName)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "FORM-941", t.Year, $"Q{t.Quarter}", ein, employerName);
        sb.AppendLine(Csv("Line1", "WagesTipsAndOtherCompensation", Fmt(t.TotalWages)));
        sb.AppendLine(Csv("Line2", "IncomeTaxWithheldFromWages", Fmt(t.IncomeTaxWithheld)));
        sb.AppendLine(Csv("Line5a", "TaxableSocialSecurityWages", Fmt(t.SocialSecurityWages)));
        sb.AppendLine(Csv("Line5aCol2", "SocialSecurityTaxEmployee", Fmt(t.SocialSecurityTax)));
        sb.AppendLine(Csv("Line5b", "MedicareWagesAndTips", Fmt(t.MedicareWages)));
        sb.AppendLine(Csv("Line5bCol2", "MedicareTaxEmployee", Fmt(t.MedicareTax)));
        sb.AppendLine(Csv("Line6", "TotalSocialSecurityAndMedicareTaxes", Fmt(t.SocialSecurityTax + t.MedicareTax + t.EmployerSocialSecurityTax + t.EmployerMedicareTax)));
        return sb.ToString();
    }

    public static string BuildForm940(Form940Totals t, string ein, string employerName)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "FORM-940", t.Year, "ANNUAL", ein, employerName);
        sb.AppendLine(Csv("Line1", "TotalPaymentsToAllEmployees", Fmt(t.TotalWages)));
        sb.AppendLine(Csv("Line3", "PaymentsExemptFromFuta", Fmt(t.FutaExemptWages)));
        sb.AppendLine(Csv("Line4", "TaxableFutaWages", Fmt(t.FutaTaxableWages)));
        sb.AppendLine(Csv("Line5", "FutaRate", Fmt4(t.FutaRate)));
        sb.AppendLine(Csv("Line6", "FutaTaxBeforeAdjustments", Fmt(t.FutaTax)));
        return sb.ToString();
    }

    public static string BuildW2(IEnumerable<W2Row> rows, int year, string ein, string employerName)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "W2", year, "ANNUAL", ein, employerName);
        foreach (var r in rows.OrderBy(r => r.EmployeeCode))
        {
            sb.AppendLine(Csv(
                r.EmployeeCode,
                r.EmployeeName,
                r.SsnMasked,
                Fmt(r.Box1Wages),
                Fmt(r.Box2FederalIncomeTax),
                Fmt(r.Box3SocialSecurityWages),
                Fmt(r.Box4SocialSecurityTax),
                Fmt(r.Box5MedicareWages),
                Fmt(r.Box6MedicareTax),
                r.StateCode,
                Fmt(r.StateWages)));
        }

        return sb.ToString();
    }

    public static string BuildW3(IReadOnlyCollection<W2Row> rows, int year, string ein, string employerName)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "W3", year, "ANNUAL", ein, employerName);
        sb.AppendLine(Csv("ControlNumber", "00000"));
        sb.AppendLine(Csv("EstablishmentCount", rows.Count > 0 ? "1" : "0"));
        sb.AppendLine(Csv("W2Count", rows.Count.ToString(CultureInfo.InvariantCulture)));
        sb.AppendLine(Csv("Box1WagesTips", Fmt(rows.Sum(r => r.Box1Wages))));
        sb.AppendLine(Csv("Box2FederalIncomeTax", Fmt(rows.Sum(r => r.Box2FederalIncomeTax))));
        sb.AppendLine(Csv("Box3SocialSecurityWages", Fmt(rows.Sum(r => r.Box3SocialSecurityWages))));
        sb.AppendLine(Csv("Box4SocialSecurityTax", Fmt(rows.Sum(r => r.Box4SocialSecurityTax))));
        sb.AppendLine(Csv("Box5MedicareWages", Fmt(rows.Sum(r => r.Box5MedicareWages))));
        sb.AppendLine(Csv("Box6MedicareTax", Fmt(rows.Sum(r => r.Box6MedicareTax))));
        return sb.ToString();
    }

    public static string BuildStateQuarterly(
        IReadOnlyCollection<StateQuarterlyRow> rows, int year, int quarter, string stateAgency)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, "STATE-QUARTERLY", year, $"Q{quarter}", string.Empty, stateAgency);
        foreach (var r in rows.OrderBy(r => r.StateCode))
        {
            sb.AppendLine(Csv(r.StateCode, Fmt(r.TotalWages), Fmt(r.ExcessWages), Fmt(r.Withholding)));
        }

        return sb.ToString();
    }

    public static decimal SplitSocialSecurityTax(decimal combinedEmployeeTax) =>
        Math.Round(combinedEmployeeTax * SocialSecurityComponent / CombinedFicaRate, 2, MidpointRounding.AwayFromZero);

    private static void AppendHeader(StringBuilder sb, string form, int year, string period, string ein, string employerName)
    {
        sb.AppendLine(Csv(form, year.ToString(CultureInfo.InvariantCulture), period, ein, employerName));
    }

    private static string Csv(params string[] values) => string.Join(",", values.Select(Escape));

    private static string Escape(string value)
    {
        if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }

    private static string Fmt(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);

    private static string Fmt4(decimal value) => value.ToString("F4", CultureInfo.InvariantCulture);
}
