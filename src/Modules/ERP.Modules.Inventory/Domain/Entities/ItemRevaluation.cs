// <copyright file="ItemRevaluation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemRevaluation : AuditableEntity
{
    private readonly List<ItemRevaluationLine> _lines = [];

    protected ItemRevaluation() { }

    public ItemRevaluation(
        Guid companyId,
        string revaluationNumber,
        DateTime revaluationDate,
        RevaluationMethod method,
        Guid? standardCostAccountId,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(revaluationNumber))
            throw new ArgumentException("Revaluation number is required.", nameof(revaluationNumber));

        CompanyId = companyId;
        RevaluationNumber = revaluationNumber;
        RevaluationDate = revaluationDate;
        Method = method;
        StandardCostAccountId = standardCostAccountId;
        Notes = notes;
        Status = ItemRevaluationStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public string RevaluationNumber { get; private set; } = string.Empty;

    public DateTime RevaluationDate { get; private set; }

    public RevaluationMethod Method { get; private set; }

    public Guid? StandardCostAccountId { get; private set; }

    public string? Notes { get; private set; }

    public ItemRevaluationStatus Status { get; private set; }

    public decimal TotalAdjustmentValue { get; private set; }

    public IReadOnlyCollection<ItemRevaluationLine> Lines => _lines.AsReadOnly();

    public void UpdateStatus(ItemRevaluationStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    public void SetTotalAdjustmentValue(decimal value)
    {
        TotalAdjustmentValue = value;
    }

    public void AddLine(ItemRevaluationLine line)
    {
        _lines.Add(line);
    }
}

public class ItemRevaluationLine : AuditableEntity
{
    protected ItemRevaluationLine() { }

    public ItemRevaluationLine(
        Guid revaluationId,
        Guid itemId,
        Guid warehouseId,
        decimal currentQuantity,
        decimal currentStandardCost,
        decimal newStandardCost,
        decimal adjustmentValue,
        string? reasonCode = null)
        : base(Guid.NewGuid())
    {
        RevaluationId = revaluationId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        CurrentQuantity = currentQuantity;
        CurrentStandardCost = currentStandardCost;
        NewStandardCost = newStandardCost;
        AdjustmentValue = adjustmentValue;
        ReasonCode = reasonCode;
    }

    public Guid RevaluationId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public decimal CurrentQuantity { get; private set; }

    public decimal CurrentStandardCost { get; private set; }

    public decimal NewStandardCost { get; private set; }

    public decimal AdjustmentValue { get; private set; }

    public string? ReasonCode { get; private set; }

    public void UpdateNewStandardCost(decimal cost)
    {
        NewStandardCost = cost;
    }

    public void UpdateAdjustmentValue(decimal value)
    {
        AdjustmentValue = value;
    }

    public void UpdateReasonCode(string? reasonCode)
    {
        ReasonCode = reasonCode;
    }
}

public enum ItemRevaluationStatus
{
    None = 0,
    Draft = 1,
    Approved = 2,
    Posted = 3,
    Cancelled = 4,
}

public enum RevaluationMethod
{
    None = 0,
    StandardCostUpdate = 1,
    AverageCostRecalc = 2,
    FIFOLayerAdjustment = 3,
    Manual = 4,
}