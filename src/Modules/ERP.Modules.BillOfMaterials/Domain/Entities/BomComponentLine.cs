// <copyright file="BomComponentLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class BomComponentLine : AuditableEntity
{
    protected BomComponentLine() { }

    public BomComponentLine(
        Guid bomHeaderId,
        Guid componentItemId,
        decimal quantityPerParent,
        string unitOfMeasure,
        decimal? scrapFactor = null,
        int? operationSequence = null,
        Guid? workCenterId = null,
        bool isPhantom = false,
        bool isCritical = false,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (quantityPerParent <= 0)
            throw new ArgumentException("Quantity per parent must be greater than zero.", nameof(quantityPerParent));

        BomHeaderId = bomHeaderId;
        ComponentItemId = componentItemId;
        QuantityPerParent = quantityPerParent;
        UnitOfMeasure = unitOfMeasure;
        ScrapFactor = scrapFactor ?? 0m;
        OperationSequence = operationSequence ?? 10;
        WorkCenterId = workCenterId;
        IsPhantom = isPhantom;
        IsCritical = isCritical;
        Notes = notes;
    }

    public Guid BomHeaderId { get; private set; }
    public Guid ComponentItemId { get; private set; }
    public decimal QuantityPerParent { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public decimal ScrapFactor { get; private set; }
    public int OperationSequence { get; private set; }
    public Guid? WorkCenterId { get; private set; }
    public Guid? RoutingOperationId { get; private set; }
    public bool IsPhantom { get; private set; }
    public bool IsCritical { get; private set; }
    public string? Notes { get; private set; }
    public decimal? EstimatedUnitCost { get; private set; }

    /// <summary>
    /// Gets the effective quantity = QuantityPerParent × (1 + ScrapFactor/100).
    /// </summary>
    public decimal EffectiveQuantity => QuantityPerParent * (1m + (ScrapFactor / 100m));

    public void Update(
        decimal? quantityPerParent,
        string? unitOfMeasure,
        decimal? scrapFactor,
        int? operationSequence,
        Guid? workCenterId,
        bool? isPhantom,
        bool? isCritical,
        string? notes)
    {
        if (quantityPerParent.HasValue)
        {
            if (quantityPerParent.Value <= 0)
                throw new ArgumentException("Quantity per parent must be greater than zero.");
            QuantityPerParent = quantityPerParent.Value;
        }

        if (unitOfMeasure is not null)
        {
            UnitOfMeasure = unitOfMeasure;
        }

        if (scrapFactor.HasValue)
        {
            ScrapFactor = scrapFactor.Value;
        }

        if (operationSequence.HasValue)
        {
            OperationSequence = operationSequence.Value;
        }

        WorkCenterId = workCenterId;

        if (isPhantom.HasValue)
        {
            IsPhantom = isPhantom.Value;
        }

        if (isCritical.HasValue)
        {
            IsCritical = isCritical.Value;
        }

        if (notes is not null)
        {
            Notes = notes;
        }
    }

    public void SetEstimatedUnitCost(decimal cost) => EstimatedUnitCost = cost;

    public void SetRoutingOperation(Guid? routingOperationId) => RoutingOperationId = routingOperationId;

    public void ReplaceComponent(Guid newComponentItemId) => ComponentItemId = newComponentItemId;
}
