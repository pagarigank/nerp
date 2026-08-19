// <copyright file="ItemExpiration.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class ItemExpiration : AuditableEntity
{
    private readonly List<ItemExpirationAlert> _alerts = [];

    protected ItemExpiration() { }

    public ItemExpiration(
        Guid companyId,
        Guid itemId,
        Guid warehouseId,
        Guid? lotId,
        Guid? serialNumberId,
        DateTime expirationDate,
        decimal quantity,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ItemId = itemId;
        WarehouseId = warehouseId;
        LotId = lotId;
        SerialNumberId = serialNumberId;
        ExpirationDate = expirationDate;
        Quantity = quantity;
        Notes = notes;
        Status = ItemExpirationStatus.Active;
    }

    public Guid CompanyId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid? LotId { get; private set; }

    public Guid? SerialNumberId { get; private set; }

    public DateTime ExpirationDate { get; private set; }

    public decimal Quantity { get; private set; }

    public string? Notes { get; private set; }

    public ItemExpirationStatus Status { get; private set; }

    public IReadOnlyCollection<ItemExpirationAlert> Alerts => _alerts.AsReadOnly();

    public void UpdateStatus(ItemExpirationStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateQuantity(decimal quantity)
    {
        Quantity = quantity;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    public void AddAlert(ItemExpirationAlert alert)
    {
        _alerts.Add(alert);
    }
}

public class ItemExpirationAlert : AuditableEntity
{
    protected ItemExpirationAlert() { }

    public ItemExpirationAlert(
        Guid itemExpirationId,
        ExpirationAlertType alertType,
        DateTime alertDate,
        string message,
        Guid? acknowledgedBy = null)
        : base(Guid.NewGuid())
    {
        ItemExpirationId = itemExpirationId;
        AlertType = alertType;
        AlertDate = alertDate;
        Message = message;
        AcknowledgedBy = acknowledgedBy;
        IsAcknowledged = acknowledgedBy.HasValue;
    }

    public Guid ItemExpirationId { get; private set; }

    public ExpirationAlertType AlertType { get; private set; }

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

public enum ItemExpirationStatus
{
    None = 0,
    Active = 1,
    Alerted = 2,
    Expired = 3,
    Disposed = 4,
    Removed = 5,
}

public enum ExpirationAlertType
{
    None = 0,
    Warning = 1,      // 30 days before expiration
    Critical = 2,     // 7 days before expiration
    Expired = 3,      // On expiration date
    Disposed = 4,     // After disposal
}