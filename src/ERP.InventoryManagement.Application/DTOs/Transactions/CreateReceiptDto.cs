// <copyright file="CreateReceiptDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating an inventory receipt transaction.
/// </summary>
public record CreateReceiptDto
{
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? LocationId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? LotNumber { get; init; }
    public string? SerialNumber { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public string? ReferenceType { get; init; } // "PO", "MFG", "ADJ", etc.
    public Guid? ReferenceId { get; init; }
    public string? Notes { get; init; }
}
