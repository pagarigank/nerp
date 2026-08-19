// <copyright file="PhysicalCount.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

public class PhysicalCount : AuditableEntity
{
    private readonly List<PhysicalCountLine> _lines = [];

    protected PhysicalCount() { }

    public PhysicalCount(
        Guid companyId,
        Guid warehouseId,
        string countNumber,
        DateTime countDate,
        PhysicalCountStatus status = PhysicalCountStatus.Draft,
        bool blindCount = false,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(countNumber))
            throw new ArgumentException("Count number is required.", nameof(countNumber));

        CompanyId = companyId;
        WarehouseId = warehouseId;
        CountNumber = countNumber;
        CountDate = countDate;
        Status = status;
        BlindCount = blindCount;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public string CountNumber { get; private set; } = string.Empty;

    public DateTime CountDate { get; private set; }

    public PhysicalCountStatus Status { get; private set; }

    public bool BlindCount { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<PhysicalCountLine> Lines => _lines.AsReadOnly();

    public void AddLine(PhysicalCountLine line)
    {
        _lines.Add(line);
    }

    public void UpdateStatus(PhysicalCountStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}

public class PhysicalCountLine : AuditableEntity
{
    protected PhysicalCountLine() { }

    public PhysicalCountLine(
        Guid physicalCountId,
        Guid itemId,
        Guid? binId,
        decimal systemQuantity,
        decimal? countedQuantity,
        string? lotNumber,
        string? serialNumber,
        string? notes)
        : base(Guid.NewGuid())
    {
        PhysicalCountId = physicalCountId;
        ItemId = itemId;
        BinId = binId;
        SystemQuantity = systemQuantity;
        CountedQuantity = countedQuantity;
        LotNumber = lotNumber;
        SerialNumber = serialNumber;
        Notes = notes;
    }

    public Guid PhysicalCountId { get; private set; }

    public Guid ItemId { get; private set; }

    public Guid? BinId { get; private set; }

    public decimal SystemQuantity { get; private set; }

    public decimal? CountedQuantity { get; private set; }

    public string? LotNumber { get; private set; }

    public string? SerialNumber { get; private set; }

    public string? Notes { get; private set; }

    public decimal? Variance => CountedQuantity.HasValue ? CountedQuantity.Value - SystemQuantity : null;

    public void SetCountedQuantity(decimal quantity)
    {
        CountedQuantity = quantity;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}

public enum PhysicalCountStatus
{
    None = 0,
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    Posted = 4,
    Cancelled = 5,
}