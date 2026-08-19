// <copyright file="BuildOrderLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class BuildOrderLine : AuditableEntity
{
    protected BuildOrderLine() { }

    public BuildOrderLine(
        Guid buildOrderId,
        Guid componentItemId,
        decimal quantityRequired,
        decimal quantityIssued,
        string unitOfMeasure,
        decimal unitCost,
        bool isLabor = false,
        bool isOverhead = false,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        BuildOrderId = buildOrderId;
        ComponentItemId = componentItemId;
        QuantityRequired = quantityRequired;
        QuantityIssued = quantityIssued;
        UnitOfMeasure = unitOfMeasure;
        UnitCost = unitCost;
        ExtendedCost = quantityIssued * unitCost;
        IsLabor = isLabor;
        IsOverhead = isOverhead;
        Notes = notes;
    }

    public Guid BuildOrderId { get; private set; }
    public Guid ComponentItemId { get; private set; }
    public decimal QuantityRequired { get; private set; }
    public decimal QuantityIssued { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal UnitCost { get; private set; }
    public decimal ExtendedCost { get; private set; }
    public bool IsLabor { get; private set; }
    public bool IsOverhead { get; private set; }
    public decimal? VarianceQuantity { get; private set; }
    public decimal? VarianceCost { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateIssuedQuantity(decimal quantityIssued, decimal unitCost)
    {
        QuantityIssued = quantityIssued;
        UnitCost = unitCost;
        ExtendedCost = quantityIssued * unitCost;
    }

    public void CalculateVariance()
    {
        VarianceQuantity = QuantityIssued - QuantityRequired;
        VarianceCost = VarianceQuantity * UnitCost;
    }
}
