// <copyright file="SerialNumber.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class SerialNumber : AuditableEntity
{
    protected SerialNumber() { }

    public SerialNumber(
        string serialNo,
        Guid itemId,
        Guid warehouseId,
        DateTime receivedDate,
        string? warrantyInfo = null,
        DateTime? installationDate = null,
        Guid? customerId = null,
        SerialStatus status = SerialStatus.InStock)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(serialNo))
            throw new ArgumentException("Serial number is required.", nameof(serialNo));

        SerialNo = serialNo;
        ItemId = itemId;
        WarehouseId = warehouseId;
        ReceivedDate = receivedDate;
        WarrantyInfo = warrantyInfo;
        InstallationDate = installationDate;
        CustomerId = customerId;
        Status = status;
    }

    public string SerialNo { get; private set; } = string.Empty;

    public Guid ItemId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public DateTime ReceivedDate { get; private set; }

    public string? WarrantyInfo { get; private set; }

    public DateTime? InstallationDate { get; private set; }

    public Guid? CustomerId { get; private set; }

    public SerialStatus Status { get; private set; }

    public void Ship(Guid customerId)
    {
        CustomerId = customerId;
        Status = SerialStatus.Shipped;
    }

    public void Install(DateTime installationDate)
    {
        InstallationDate = installationDate;
        Status = SerialStatus.Installed;
    }

    public void Return()
    {
        Status = SerialStatus.Returned;
    }
}

public enum SerialStatus
{
    None = 0,
    InStock = 1,
    Shipped = 2,
    Installed = 3,
    Returned = 4,
}
