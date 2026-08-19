// <copyright file="Item.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class Item : AuditableEntity
{
    private readonly List<ItemAlternateCode> _alternateCodes = [];
    private readonly List<ItemUnitOfMeasureConversion> _uomConversions = [];

    protected Item() { }

    public Item(
        string itemCode,
        string description,
        Guid companyId,
        ItemType itemType,
        string baseUnitOfMeasure,
        CostingMethod costingMethod,
        Guid itemCategoryId,
        ItemStatus status = ItemStatus.Active)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(itemCode))
            throw new ArgumentException("Item code is required.", nameof(itemCode));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        ItemCode = itemCode;
        Description = description;
        CompanyId = companyId;
        ItemType = itemType;
        BaseUnitOfMeasure = baseUnitOfMeasure;
        CostingMethod = costingMethod;
        ItemCategoryId = itemCategoryId;
        Status = status;
        AllowNegativeInventory = false;
        IsLotControlled = false;
        IsSerialControlled = false;
    }

    public string ItemCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? LongDescription { get; private set; }
    public Guid CompanyId { get; private set; }
    public ItemType ItemType { get; private set; }
    public string BaseUnitOfMeasure { get; private set; } = string.Empty;
    public CostingMethod CostingMethod { get; private set; }
    public Guid ItemCategoryId { get; private set; }
    public ItemStatus Status { get; private set; }
    public bool AllowNegativeInventory { get; private set; }
    public bool IsLotControlled { get; private set; }
    public bool IsSerialControlled { get; private set; }
    public bool IsKit { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public decimal? Height { get; private set; }
    public string? WeightUnit { get; private set; }
    public bool IsHazardousMaterial { get; private set; }
    public string? HazardClass { get; private set; }
    public string? CountryOfOrigin { get; private set; }
    public string? HsCode { get; private set; }
    public string? StorageCondition { get; private set; }
    public decimal? StandardCost { get; private set; }
    public decimal? ReorderPoint { get; private set; }
    public decimal? ReorderQuantity { get; private set; }
    public decimal? SafetyStock { get; private set; }
    public int? LeadTimeDays { get; private set; }
    public string? ABCClass { get; private set; }
    public IReadOnlyCollection<ItemAlternateCode> AlternateCodes => _alternateCodes.AsReadOnly();
    public IReadOnlyCollection<ItemUnitOfMeasureConversion> UOMConversions => _uomConversions;

    public void UpdateDescription(string description, string? longDescription = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        Description = description;
        LongDescription = longDescription;
    }

    public void UpdateCostingMethod(CostingMethod newMethod)
    {
        CostingMethod = newMethod;
    }

    public void UpdateStandardCost(decimal cost)
    {
        if (cost < 0)
        {
            throw new ArgumentException("Cost cannot be negative.", nameof(cost));
        }

        StandardCost = cost;
    }

    public void UpdateReorderParameters(decimal? reorderPoint, decimal? reorderQty, decimal? safetyStock, int? leadTimeDays)
    {
        ReorderPoint = reorderPoint;
        ReorderQuantity = reorderQty;
        SafetyStock = safetyStock;
        LeadTimeDays = leadTimeDays;
    }

    public void SetLotControlled(bool isLotControlled)
    {
        IsLotControlled = isLotControlled;
    }

    public void SetSerialControlled(bool isSerialControlled)
    {
        IsSerialControlled = isSerialControlled;
    }

    public void SetKit(bool isKit)
    {
        IsKit = isKit;
    }

    public void UpdatePhysicalAttributes(
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        string? weightUnit,
        bool isHazardousMaterial,
        string? hazardClass,
        string? countryOfOrigin,
        string? hsCode,
        string? storageCondition)
    {
        Weight = weight;
        Length = length;
        Width = width;
        Height = height;
        WeightUnit = weightUnit;
        IsHazardousMaterial = isHazardousMaterial;
        HazardClass = hazardClass;
        CountryOfOrigin = countryOfOrigin;
        HsCode = hsCode;
        StorageCondition = storageCondition;
    }

    public void AllowNegative(bool allow)
    {
        AllowNegativeInventory = allow;
    }

    public void UpdateABCClass(string abcClass)
    {
        ABCClass = abcClass;
    }

    public void Activate()
    {
        Status = ItemStatus.Active;
    }

    public void Deactivate()
    {
        Status = ItemStatus.Inactive;
    }

    public void Discontinue()
    {
        Status = ItemStatus.Discontinued;
    }

    public void AddAlternateCode(ItemAlternateCode alternateCode)
    {
        _alternateCodes.Add(alternateCode);
    }

    public void AddUOMConversion(ItemUnitOfMeasureConversion conversion)
    {
        _uomConversions.Add(conversion);
    }
}

public enum ItemType
{
    None = 0,
    Inventory = 1,
    NonInventory = 2,
    Service = 3,
}

public enum CostingMethod
{
    None = 0,
    FIFO = 1,
    LIFO = 2,
    Average = 3,
    Standard = 4,
    LotSpecific = 5,
}

public enum ItemStatus
{
    None = 0,
    Active = 1,
    Inactive = 2,
    Discontinued = 3,
}
