// <copyright file="ProcessPhysicalCountDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for processing physical count results.
/// </summary>
public record ProcessPhysicalCountDto
{
    public Guid WarehouseId { get; init; }
    public DateTime CountDate { get; init; }
    public List<PhysicalCountLineDto> Lines { get; init; } = new();
    public bool AutoCreateAdjustments { get; init; } = true;
}

public record PhysicalCountLineDto
{
    public Guid ItemId { get; init; }
    public Guid? LocationId { get; init; }
    public string? LotNumber { get; init; }
    public string? SerialNumber { get; init; }
    public decimal CountedQuantity { get; init; }
    public string? Notes { get; init; }
}
