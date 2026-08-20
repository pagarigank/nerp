// <copyright file="VanStock.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public class VanStock : AuditableEntity
{
    protected VanStock()
    {
    }

    public VanStock(
        Guid companyId,
        Guid technicianId,
        Guid itemId,
        Guid warehouseId,
        decimal quantityOnHand,
        decimal reorderPoint)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        TechnicianId = technicianId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        QuantityOnHand = quantityOnHand;
        ReorderPoint = reorderPoint;
    }

    public Guid CompanyId { get; private set; }

    public Guid TechnicianId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public decimal QuantityOnHand { get; private set; }

    public decimal ReorderPoint { get; private set; }

    public void IssueParts(decimal qty) => QuantityOnHand -= qty;

    public void ReceiveParts(decimal qty) => QuantityOnHand += qty;
}

public class WarrantyClaim : AuditableEntity
{
    protected WarrantyClaim()
    {
    }

    public WarrantyClaim(
        Guid companyId,
        string claimNumber,
        Guid equipmentAssetId,
        Guid? workOrderId,
        string description,
        decimal claimAmount)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        ClaimNumber = claimNumber;
        EquipmentAssetId = equipmentAssetId;
        WorkOrderId = workOrderId;
        Description = description;
        ClaimAmount = claimAmount;
    }

    public Guid CompanyId { get; private set; }

    public string ClaimNumber { get; private set; } = string.Empty;

    public Guid EquipmentAssetId { get; private set; }

    public Guid? WorkOrderId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal ClaimAmount { get; private set; }

    public string Status { get; private set; } = "Open";

    public DateTime? DecisionDate { get; private set; }

    public void Approve()
    {
        Status = "Approved";
        DecisionDate = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = "Rejected";
        DecisionDate = DateTime.UtcNow;
    }

    public void Reimburse() => Status = "Reimbursed";
}
