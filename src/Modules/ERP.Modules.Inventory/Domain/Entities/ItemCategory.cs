// <copyright file="ItemCategory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemCategory : AuditableEntity
{
    protected ItemCategory() { }

    public ItemCategory(
        string categoryCode,
        string categoryName,
        Guid companyId,
        Guid? inventoryAccountId,
        Guid? cogsAccountId,
        Guid? varianceAccountId,
        string? description = null,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required.", nameof(categoryCode));
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name is required.", nameof(categoryName));

        CategoryCode = categoryCode;
        CategoryName = categoryName;
        CompanyId = companyId;
        InventoryAccountId = inventoryAccountId;
        COGSAccountId = cogsAccountId;
        VarianceAccountId = varianceAccountId;
        Description = description;
        IsActive = isActive;
    }

    public string CategoryCode { get; private set; } = string.Empty;

    public string CategoryName { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public Guid? InventoryAccountId { get; private set; }

    public Guid? COGSAccountId { get; private set; }

    public Guid? VarianceAccountId { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateAccounts(Guid? inventoryAccountId, Guid? cogsAccountId, Guid? varianceAccountId)
    {
        InventoryAccountId = inventoryAccountId;
        COGSAccountId = cogsAccountId;
        VarianceAccountId = varianceAccountId;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
