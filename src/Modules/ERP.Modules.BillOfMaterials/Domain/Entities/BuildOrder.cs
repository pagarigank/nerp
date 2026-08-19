// <copyright file="BuildOrder.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class BuildOrder : AuditableEntity
{
    private readonly List<BuildOrderLine> _lines = [];

    protected BuildOrder() { }

    public BuildOrder(
        Guid companyId,
        string buildNumber,
        BuildTransactionType transactionType,
        Guid bomHeaderId,
        Guid parentItemId,
        decimal quantityToBuild,
        string unitOfMeasure,
        Guid warehouseId,
        DateTime buildDate,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(buildNumber))
            throw new ArgumentException("Build number is required.", nameof(buildNumber));
        if (quantityToBuild <= 0)
            throw new ArgumentException("Quantity to build must be greater than zero.", nameof(quantityToBuild));

        CompanyId = companyId;
        BuildNumber = buildNumber;
        TransactionType = transactionType;
        BomHeaderId = bomHeaderId;
        ParentItemId = parentItemId;
        QuantityToBuild = quantityToBuild;
        UnitOfMeasure = unitOfMeasure;
        WarehouseId = warehouseId;
        BuildDate = buildDate;
        Status = BuildOrderStatus.Draft;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }
    public string BuildNumber { get; private set; } = string.Empty;
    public BuildTransactionType TransactionType { get; private set; }
    public Guid BomHeaderId { get; private set; }
    public Guid ParentItemId { get; private set; }
    public decimal QuantityToBuild { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public DateTime BuildDate { get; private set; }
    public BuildOrderStatus Status { get; private set; }
    public decimal? ActualYield { get; set; }
    public decimal? TotalMaterialCost { get; private set; }
    public decimal? TotalLaborCost { get; private set; }
    public decimal? TotalOverheadCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public decimal? UnitCost { get; private set; }
    public string? Notes { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTime? PostedDate { get; private set; }

    public IReadOnlyCollection<BuildOrderLine> Lines => _lines.AsReadOnly();

    public void UpdateStatus(BuildOrderStatus status)
    {
        Status = status;
    }

    public void SetPosted(Guid postedBy)
    {
        PostedBy = postedBy;
        PostedDate = DateTime.UtcNow;
        Status = BuildOrderStatus.Completed;
    }

    public void CalculateCosts()
    {
        TotalMaterialCost = _lines.Where(l => !l.IsOverhead).Sum(l => l.ExtendedCost);
        TotalLaborCost = _lines.Where(l => l.IsLabor).Sum(l => l.ExtendedCost);
        TotalOverheadCost = _lines.Where(l => l.IsOverhead).Sum(l => l.ExtendedCost);
        TotalCost = TotalMaterialCost + TotalLaborCost + TotalOverheadCost;
        UnitCost = QuantityToBuild > 0 ? TotalCost / QuantityToBuild : 0m;
    }

    public BuildOrderLine AddLine(
        Guid componentItemId,
        decimal quantityRequired,
        decimal quantityIssued,
        string unitOfMeasure,
        decimal unitCost,
        bool isLabor = false,
        bool isOverhead = false,
        string? notes = null)
    {
        var line = new BuildOrderLine(
            Id,
            componentItemId,
            quantityRequired,
            quantityIssued,
            unitOfMeasure,
            unitCost,
            isLabor,
            isOverhead,
            notes);
        _lines.Add(line);
        return line;
    }
}
