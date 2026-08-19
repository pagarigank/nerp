// <copyright file="CreateIssueDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for creating an inventory issue transaction.
/// </summary>
public record CreateIssueDto
{
    public Guid ItemId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? LocationId { get; init; }
    public decimal Quantity { get; init; }
    public string? LotNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? ReferenceType { get; init; } // "SO", "WO", "ADJ", etc.
    public Guid? ReferenceId { get; init; }
    public string? Notes { get; init; }
}
