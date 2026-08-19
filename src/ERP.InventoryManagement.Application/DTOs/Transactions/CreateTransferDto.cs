// <copyright file="CreateTransferDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating an inventory transfer transaction.
/// </summary>
public record CreateTransferDto
{
    public Guid ItemId { get; init; }
    public Guid FromWarehouseId { get; init; }
    public Guid? FromLocationId { get; init; }
    public Guid ToWarehouseId { get; init; }
    public Guid? ToLocationId { get; init; }
    public decimal Quantity { get; init; }
    public string? LotNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? Notes { get; init; }
}
