// <copyright file="UnitOfMeasure.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

/// <summary>
/// Global Unit of Measure master. Defines a UOM code (e.g. CS, BOX, EA, KG) and its
/// equivalence to a base UOM (e.g. 1 CS = 12 EA) so transactional lines can select a
/// UOM from a controlled list instead of free text.
/// </summary>
public class UnitOfMeasure : AuditableEntity
{
    protected UnitOfMeasure() { }

    public UnitOfMeasure(
        Guid companyId,
        string code,
        string description,
        string baseUOM,
        decimal factorToBase)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("UOM code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(baseUOM))
            throw new ArgumentException("Base UOM is required.", nameof(baseUOM));
        if (factorToBase <= 0)
            throw new ArgumentException("Factor to base must be positive.", nameof(factorToBase));

        CompanyId = companyId;
        Code = code;
        Description = description;
        BaseUOM = baseUOM;
        FactorToBase = factorToBase;
    }

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>The UOM that this unit is expressed in terms of (e.g. EA).</summary>
    public string BaseUOM { get; private set; } = string.Empty;

    /// <summary>How many of the base UOM equal one of this UOM (e.g. 12 for CS -> 12 EA).</summary>
    public decimal FactorToBase { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(string description, string baseUOM, decimal factorToBase, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(baseUOM))
            throw new ArgumentException("Base UOM is required.", nameof(baseUOM));
        if (factorToBase <= 0)
            throw new ArgumentException("Factor to base must be positive.", nameof(factorToBase));

        Description = description;
        BaseUOM = baseUOM;
        FactorToBase = factorToBase;
        IsActive = isActive;
    }
}
