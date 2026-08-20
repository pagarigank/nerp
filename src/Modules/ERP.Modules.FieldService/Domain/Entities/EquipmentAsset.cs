// <copyright file="EquipmentAsset.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum EquipmentOwnership
{
    CustomerOwned,
    CompanyOwned
}

public class EquipmentAsset : AuditableEntity
{
    protected EquipmentAsset()
    {
    }

    public EquipmentAsset(
        Guid companyId,
        string assetTag,
        string serialNumber,
        string description,
        Guid? itemId,
        Guid? customerId,
        Guid? locationId,
        EquipmentOwnership ownership,
        DateTime? installDate,
        DateTime? warrantyStart,
        DateTime? warrantyEnd,
        bool underWarranty,
        string? notes)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        AssetTag = assetTag;
        SerialNumber = serialNumber;
        Description = description;
        ItemId = itemId;
        CustomerId = customerId;
        LocationId = locationId;
        Ownership = ownership;
        InstallDate = installDate;
        WarrantyStart = warrantyStart;
        WarrantyEnd = warrantyEnd;
        UnderWarranty = underWarranty;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }

    public string AssetTag { get; private set; } = string.Empty;

    public string SerialNumber { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? ItemId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? LocationId { get; private set; }

    public EquipmentOwnership Ownership { get; private set; }

    public DateTime? InstallDate { get; private set; }

    public DateTime? WarrantyStart { get; private set; }

    public DateTime? WarrantyEnd { get; private set; }

    public bool UnderWarranty { get; private set; }

    public string? Notes { get; private set; }

    public void MarkWarranty(bool underWarranty) => UnderWarranty = underWarranty;

    public void UpdateWarrantyDates(DateTime? start, DateTime? end)
    {
        WarrantyStart = start;
        WarrantyEnd = end;
    }
}
