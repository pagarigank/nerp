// <copyright file="Warehouse.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class Warehouse : AuditableEntity
{
    protected Warehouse() { }

    public Warehouse(
        string warehouseCode,
        string warehouseName,
        Guid companyId,
        WarehouseType warehouseType,
        string? address = null,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(warehouseCode))
            throw new ArgumentException("Warehouse code is required.", nameof(warehouseCode));
        if (string.IsNullOrWhiteSpace(warehouseName))
            throw new ArgumentException("Warehouse name is required.", nameof(warehouseName));

        WarehouseCode = warehouseCode;
        WarehouseName = warehouseName;
        CompanyId = companyId;
        WarehouseType = warehouseType;
        Address = address;
        IsActive = isActive;
    }

    public string WarehouseCode { get; private set; } = string.Empty;

    public string WarehouseName { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public WarehouseType WarehouseType { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateAddress(string? address) => Address = address;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

public enum WarehouseType
{
    None = 0,
    Distribution = 1,
    Manufacturing = 2,
    Service = 3,
    Transit = 4,
}
