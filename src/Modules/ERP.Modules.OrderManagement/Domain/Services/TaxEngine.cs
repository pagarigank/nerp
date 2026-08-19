// <copyright file="TaxEngine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Linq;
using ERP.Modules.OrderManagement.Domain.Entities;

namespace ERP.Modules.OrderManagement.Domain.Services;

/// <summary>
/// Resolves the applicable tax rate for a (jurisdiction, date) context and
/// computes line tax. Honors item taxability and customer exemption:
/// exempt customers always pay 0 tax regardless of code.
/// </summary>
public static class TaxEngine
{
    public static TaxResult CalculateTax(
        decimal taxableAmount,
        string? jurisdiction,
        bool itemTaxable,
        bool customerExempt,
        IReadOnlyList<TaxCode> taxCodes,
        DateTime asOf)
    {
        if (customerExempt || !itemTaxable)
            return new TaxResult(0m, null, true);

        var code = taxCodes
            .Where(t => t.IsEffectiveOn(asOf))
            .Where(t => string.Equals(t.Jurisdiction, jurisdiction, System.StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.EffectiveFrom)
            .FirstOrDefault();

        if (code is null || !code.IsTaxable)
            return new TaxResult(0m, null, false);

        var tax = taxableAmount * (code.Rate / 100m);
        return new TaxResult(tax, code.Id, false);
    }
}

public record TaxResult(decimal TaxAmount, Guid? AppliedTaxCodeId, bool Exempt);
