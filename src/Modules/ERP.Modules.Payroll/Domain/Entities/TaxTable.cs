// <copyright file="TaxTable.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Tax bracket/rate table for a jurisdiction (federal, state, local) and year.
/// Used by the withholding engine for bracket-style computation.
/// </summary>
public class TaxTable : AuditableEntity
{
    private readonly List<TaxBracket> _brackets = [];

    protected TaxTable() { }

    public TaxTable(
        Guid companyId,
        string name,
        TaxJurisdictionLevel level,
        string? stateCode,
        int year,
        FilingStatus filingStatus,
        decimal? standardDeduction)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tax table name is required.", nameof(name));

        CompanyId = companyId;
        Name = name;
        Level = level;
        StateCode = stateCode;
        Year = year;
        FilingStatus = filingStatus;
        StandardDeduction = standardDeduction;
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TaxJurisdictionLevel Level { get; private set; }
    public string? StateCode { get; private set; }
    public int Year { get; private set; }
    public FilingStatus FilingStatus { get; private set; }
    public decimal? StandardDeduction { get; private set; }

    public IReadOnlyCollection<TaxBracket> Brackets => _brackets.AsReadOnly();

    public void AddBracket(decimal rate, decimal lowerBound, decimal? upperBound, decimal? fixedAmount)
    {
        _brackets.Add(new TaxBracket(rate, lowerBound, upperBound, fixedAmount));
    }
}

/// <summary>A single progressive bracket within a <see cref="TaxTable"/>.</summary>
public class TaxBracket : AuditableEntity
{
    protected TaxBracket() { }

    public TaxBracket(decimal rate, decimal lowerBound, decimal? upperBound, decimal? fixedAmount)
        : base(Guid.NewGuid())
    {
        Rate = rate;
        LowerBound = lowerBound;
        UpperBound = upperBound;
        FixedAmount = fixedAmount;
    }

    /// <summary>Marginal rate (e.g. 0.22).</summary>
    public decimal Rate { get; private set; }
    /// <summary>Lower bound of taxable income for this bracket.</summary>
    public decimal LowerBound { get; private set; }
    /// <summary>Upper bound (null = top bracket).</summary>
    public decimal? UpperBound { get; private set; }
    /// <summary>Fixed base tax before applying the marginal rate (optional).</summary>
    public decimal? FixedAmount { get; private set; }

    /// <summary>Shadow FK populated by EF (configured in PayrollDbContext).</summary>
    public Guid TaxTableId { get; private set; }
}
