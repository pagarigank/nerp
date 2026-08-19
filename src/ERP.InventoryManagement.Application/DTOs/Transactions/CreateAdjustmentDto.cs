// <copyright file="CreateAdjustmentDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating an inventory adjustment transaction.
/// </summary>
public record CreateAdjustmentDto
{
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? LocationId { get; init; }
    public decimal QuantityAdjustment { get; init; } // Can be positive or negative
    public string ReasonCode { get; init; } = string.Empty;
    public string? LotNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? Notes { get; init; }
}
