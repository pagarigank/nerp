// <copyright file="SlowMovingAlert.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class SlowMovingAlert : AuditableEntity
{
    protected SlowMovingAlert() { }

    public SlowMovingAlert(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        decimal onHandQuantity,
        int daysSinceLastMovement,
        string message)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        OnHandQuantity = onHandQuantity;
        DaysSinceLastMovement = daysSinceLastMovement;
        Message = message;
        AlertDate = DateTime.UtcNow;
        IsAcknowledged = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal OnHandQuantity { get; private set; }
    public int DaysSinceLastMovement { get; private set; }
    public DateTime AlertDate { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsAcknowledged { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedDate { get; private set; }

    public void Acknowledge(Guid acknowledgedBy)
    {
        IsAcknowledged = true;
        AcknowledgedBy = acknowledgedBy;
        AcknowledgedDate = DateTime.UtcNow;
    }
}