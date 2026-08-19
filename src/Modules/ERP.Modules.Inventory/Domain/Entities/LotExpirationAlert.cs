// <copyright file="LotExpirationAlert.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class LotExpirationAlert : AuditableEntity
{
    protected LotExpirationAlert() { }

    public LotExpirationAlert(
        Guid companyId,
        Guid lotId,
        Guid itemId,
        Guid warehouseId,
        LotExpirationAlertType alertType,
        DateTime alertDate,
        decimal availableQuantity,
        DateTime expirationDate,
        string message)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        LotId = lotId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        AlertType = alertType;
        AlertDate = alertDate;
        AvailableQuantity = availableQuantity;
        ExpirationDate = expirationDate;
        Message = message;
        IsAcknowledged = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid LotId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public LotExpirationAlertType AlertType { get; private set; }
    public DateTime AlertDate { get; private set; }
    public decimal AvailableQuantity { get; private set; }
    public DateTime ExpirationDate { get; private set; }
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

public enum LotExpirationAlertType
{
    None = 0,
    Warning = 1,      // 30 days before expiration
    Critical = 2,     // 7 days before expiration
    Expired = 3,      // On or after expiration date
}