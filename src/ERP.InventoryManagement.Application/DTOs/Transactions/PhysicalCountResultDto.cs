// <copyright file="PhysicalCountResultDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for physical count processing results.
/// </summary>
public record PhysicalCountResultDto
{
    public int TotalLines { get; init; }
    public int AdjustmentsCreated { get; init; }
    public List<CountVarianceDto> Variances { get; init; } = new();
}

public record CountVarianceDto
{
    public Guid ItemId { get; init; }
    public string ItemNumber { get; init; } = string.Empty;
    public decimal SystemQuantity { get; init; }
    public decimal CountedQuantity { get; init; }
    public decimal Variance { get; init; }
    public decimal VarianceValue { get; init; }
    public Guid? AdjustmentTransactionId { get; init; }
}
