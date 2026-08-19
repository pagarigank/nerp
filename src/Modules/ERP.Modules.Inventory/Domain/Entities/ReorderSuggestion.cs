// <copyright file="ReorderSuggestion.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ReorderSuggestion : AuditableEntity
{
    private readonly List<ReorderSuggestionLine> _lines = [];

    protected ReorderSuggestion() { }

    public ReorderSuggestion(
        Guid companyId,
        string suggestionNumber,
        DateTime suggestionDate,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(suggestionNumber))
            throw new ArgumentException("Suggestion number is required.", nameof(suggestionNumber));

        CompanyId = companyId;
        SuggestionNumber = suggestionNumber;
        SuggestionDate = suggestionDate;
        Notes = notes;
        Status = ReorderSuggestionStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public string SuggestionNumber { get; private set; } = string.Empty;

    public DateTime SuggestionDate { get; private set; }

    public string? Notes { get; private set; }

    public ReorderSuggestionStatus Status { get; private set; }

    public IReadOnlyCollection<ReorderSuggestionLine> Lines => _lines.AsReadOnly();

    public void AddLine(ReorderSuggestionLine line)
    {
        _lines.Add(line);
    }

    public void UpdateStatus(ReorderSuggestionStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}

public class ReorderSuggestionLine : AuditableEntity
{
    protected ReorderSuggestionLine() { }

    public ReorderSuggestionLine(
        Guid reorderSuggestionId,
        Guid itemId,
        Guid warehouseId,
        decimal currentOnHand,
        decimal currentAllocated,
        decimal availableQuantity,
        decimal reorderPoint,
        decimal safetyStock,
        decimal leadTimeDemand,
        decimal suggestedOrderQuantity,
        decimal estimatedStockoutDate,
        string? vendorId = null,
        decimal? vendorCost = null,
        int? leadTimeDays = null,
        string? priority = null)
        : base(Guid.NewGuid())
    {
        ReorderSuggestionId = reorderSuggestionId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        CurrentOnHand = currentOnHand;
        CurrentAllocated = currentAllocated;
        AvailableQuantity = availableQuantity;
        ReorderPoint = reorderPoint;
        SafetyStock = safetyStock;
        LeadTimeDemand = leadTimeDemand;
        SuggestedOrderQuantity = suggestedOrderQuantity;
        EstimatedStockoutDate = estimatedStockoutDate;
        VendorId = vendorId;
        VendorCost = vendorCost;
        LeadTimeDays = leadTimeDays;
        Priority = priority;
        Status = ReorderSuggestionLineStatus.Pending;
    }

    public Guid ReorderSuggestionId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public decimal CurrentOnHand { get; private set; }

    public decimal CurrentAllocated { get; private set; }

    public decimal AvailableQuantity { get; private set; }

    public decimal ReorderPoint { get; private set; }

    public decimal SafetyStock { get; private set; }

    public decimal LeadTimeDemand { get; private set; }

    public decimal SuggestedOrderQuantity { get; private set; }

    public decimal EstimatedStockoutDate { get; private set; }

    public string? VendorId { get; private set; }

    public decimal? VendorCost { get; private set; }

    public int? LeadTimeDays { get; private set; }

    public string? Priority { get; private set; }

    public ReorderSuggestionLineStatus Status { get; private set; }

    public void UpdateStatus(ReorderSuggestionLineStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateSuggestedOrderQuantity(decimal quantity)
    {
        SuggestedOrderQuantity = quantity;
    }

    public void UpdatePriority(string priority)
    {
        Priority = priority;
    }

    public void SetVendorInfo(string vendorId, decimal? vendorCost, int? leadTimeDays)
    {
        VendorId = vendorId;
        VendorCost = vendorCost;
        LeadTimeDays = leadTimeDays;
    }
}

public enum ReorderSuggestionStatus
{
    None = 0,
    Draft = 1,
    Approved = 2,
    ConvertedToPO = 3,
    Cancelled = 4,
}

public enum ReorderSuggestionLineStatus
{
    None = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    ConvertedToPO = 4,
}