// <copyright file="BomCoProduct.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

/// <summary>
/// Co-product / by-product of an assembly BOM: a secondary output produced alongside the
/// primary parent item, with a cost-split percentage allocating part of the joint production
/// cost to the by-product (spec §7.4 co-product by-product handling).
/// </summary>
public class BomCoProduct : AuditableEntity
{
    private readonly List<BomCoProductCost> _costSplits = [];

    protected BomCoProduct() { }

    public BomCoProduct(
        Guid bomHeaderId,
        Guid itemId,
        CoProductType coProductType,
        decimal quantityPerBuild,
        string unitOfMeasure,
        decimal? costSplitPercentage = null,
        string? description = null)
        : base(Guid.NewGuid())
    {
        BomHeaderId = bomHeaderId;
        ItemId = itemId;
        CoProductType = coProductType;
        QuantityPerBuild = quantityPerBuild;
        UnitOfMeasure = unitOfMeasure;
        CostSplitPercentage = costSplitPercentage ?? 0;
        Description = description;
    }

    public Guid BomHeaderId { get; private set; }
    public Guid ItemId { get; private set; }
    public CoProductType CoProductType { get; private set; }
    public decimal QuantityPerBuild { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;

    /// <summary>Gets the percentage of the joint production cost allocated to this co/by-product (0-100).</summary>
    public decimal CostSplitPercentage { get; private set; }

    public string? Description { get; private set; }

    public IReadOnlyCollection<BomCoProductCost> CostSplits => _costSplits.AsReadOnly();

    public void Update(
        CoProductType? coProductType,
        decimal? quantityPerBuild,
        decimal? costSplitPercentage,
        string? description)
    {
        if (coProductType.HasValue)
            CoProductType = coProductType.Value;
        if (quantityPerBuild.HasValue)
            QuantityPerBuild = quantityPerBuild.Value;
        if (costSplitPercentage.HasValue)
        {
            if (costSplitPercentage.Value < 0 || costSplitPercentage.Value > 100)
                throw new ArgumentException("Cost split percentage must be between 0 and 100.", nameof(costSplitPercentage));
            CostSplitPercentage = costSplitPercentage.Value;
        }

        if (description is not null)
            Description = description;
    }

    /// <summary>Allocates the given joint production cost to this co/by-product based on its cost-split percentage.</summary>
    /// <param name="jointCost">The total joint production cost of the parent assembly.</param>
    /// <returns>The cost allocated to this co/by-product.</returns>
    public decimal AllocateCost(decimal jointCost)
    {
        return jointCost * (CostSplitPercentage / 100m);
    }

    public BomCoProductCost AddCostSplit(string costElement, decimal allocatedAmount, string? notes = null)
    {
        var split = new BomCoProductCost(Id, costElement, allocatedAmount, notes);
        _costSplits.Add(split);
        return split;
    }

    public void RemoveCostSplit(Guid costSplitId)
    {
        var split = _costSplits.FirstOrDefault(c => c.Id == costSplitId);
        if (split is not null)
            _costSplits.Remove(split);
    }
}

/// <summary>A single allocated cost element for a co/by-product (material, labor, overhead, etc.).</summary>
public class BomCoProductCost : AuditableEntity
{
    protected BomCoProductCost() { }

    public BomCoProductCost(Guid coProductId, string costElement, decimal allocatedAmount, string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(costElement))
            throw new ArgumentException("Cost element is required.", nameof(costElement));
        CoProductId = coProductId;
        CostElement = costElement;
        AllocatedAmount = allocatedAmount;
        Notes = notes;
    }

    public Guid CoProductId { get; private set; }
    public string CostElement { get; private set; } = string.Empty;
    public decimal AllocatedAmount { get; private set; }
    public string? Notes { get; private set; }

    public void Update(decimal? allocatedAmount, string? notes)
    {
        if (allocatedAmount.HasValue)
            AllocatedAmount = allocatedAmount.Value;
        if (notes is not null)
            Notes = notes;
    }
}

public enum CoProductType
{
    CoProduct = 0,
    ByProduct = 1,
}
