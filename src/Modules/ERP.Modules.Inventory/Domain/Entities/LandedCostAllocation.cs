// <copyright file="LandedCostAllocation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class LandedCostAllocation : AuditableEntity
{
    private readonly List<LandedCostAllocationLine> _lines = [];

    protected LandedCostAllocation() { }

    public LandedCostAllocation(
        Guid companyId,
        Guid receiptTransactionId,
        string allocationNumber,
        DateTime allocationDate,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(allocationNumber))
            throw new ArgumentException("Allocation number is required.", nameof(allocationNumber));

        CompanyId = companyId;
        ReceiptTransactionId = receiptTransactionId;
        AllocationNumber = allocationNumber;
        AllocationDate = allocationDate;
        Status = LandedCostAllocationStatus.Draft;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }

    public Guid ReceiptTransactionId { get; private set; }

    public string AllocationNumber { get; private set; } = string.Empty;

    public DateTime AllocationDate { get; private set; }

    public LandedCostAllocationStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public decimal TotalAllocatedCost => _lines.Sum(l => l.AllocatedAmount);

    public IReadOnlyCollection<LandedCostAllocationLine> Lines => _lines.AsReadOnly();

    public void AddLine(LandedCostAllocationLine line)
    {
        _lines.Add(line);
    }

    public void UpdateStatus(LandedCostAllocationStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}

public class LandedCostAllocationLine : AuditableEntity
{
    protected LandedCostAllocationLine() { }

    public LandedCostAllocationLine(
        Guid landedCostAllocationId,
        Guid itemId,
        decimal quantityReceived,
        decimal unitCost,
        LandedCostAllocationMethod allocationMethod,
        decimal allocatedAmount,
        Guid? landedCostId = null,
        string? description = null)
        : base(Guid.NewGuid())
    {
        LandedCostAllocationId = landedCostAllocationId;
        ItemId = itemId;
        QuantityReceived = quantityReceived;
        UnitCost = unitCost;
        AllocationMethod = allocationMethod;
        AllocatedAmount = allocatedAmount;
        LandedCostId = landedCostId;
        Description = description;
    }

    public Guid LandedCostAllocationId { get; private set; }

    public Guid ItemId { get; private set; }

    public decimal QuantityReceived { get; private set; }

    public decimal UnitCost { get; private set; }

    public LandedCostAllocationMethod AllocationMethod { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    public Guid? LandedCostId { get; private set; }

    public string? Description { get; private set; }

    public void UpdateAllocatedAmount(decimal amount)
    {
        AllocatedAmount = amount;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void SetLandedCostId(Guid landedCostId)
    {
        LandedCostId = landedCostId;
    }
}

public class LandedCost : AuditableEntity
{
    protected LandedCost() { }

    public LandedCost(
        Guid companyId,
        Guid vendorId,
        string costCode,
        string description,
        LandedCostType costType,
        decimal amount,
        DateTime costDate,
        string? referenceNumber = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(costCode))
            throw new ArgumentException("Cost code is required.", nameof(costCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        CompanyId = companyId;
        VendorId = vendorId;
        CostCode = costCode;
        Description = description;
        CostType = costType;
        Amount = amount;
        CostDate = costDate;
        ReferenceNumber = referenceNumber;
        Status = LandedCostStatus.PendingAllocation;
    }

    public Guid CompanyId { get; private set; }

    public Guid VendorId { get; private set; }

    public string CostCode { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public LandedCostType CostType { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime CostDate { get; private set; }

    public string? ReferenceNumber { get; private set; }

    public LandedCostStatus Status { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    public decimal RemainingAmount => Amount - AllocatedAmount;

    public void UpdateStatus(LandedCostStatus newStatus)
    {
        Status = newStatus;
    }

    public void AddAllocatedAmount(decimal amount)
    {
        AllocatedAmount += amount;
    }

    public void UpdateAmount(decimal newAmount)
    {
        if (newAmount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(newAmount));
        Amount = newAmount;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        Description = description;
    }
}

public enum LandedCostAllocationStatus
{
    None = 0,
    Draft = 1,
    Posted = 2,
    Cancelled = 3,
}

public enum LandedCostAllocationMethod
{
    None = 0,
    ByQuantity = 1,
    ByValue = 2,
    ByWeight = 3,
    ByVolume = 4,
    Manual = 5,
}

public enum LandedCostType
{
    None = 0,
    Freight = 1,
    Duty = 2,
    Insurance = 3,
    Handling = 4,
    Brokerage = 5,
    Other = 99,
}

public enum LandedCostStatus
{
    None = 0,
    PendingAllocation = 1,
    PartiallyAllocated = 2,
    FullyAllocated = 3,
    Cancelled = 4,
}