// <copyright file="ValidateLotSerialDto.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.DTOs.Transactions;

/// <summary>
/// DTO for validating lot/serial requirements.
/// </summary>
public record ValidateLotSerialDto
{
    public Guid ItemId { get; init; }
    public string? LotNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string TransactionType { get; init; } = string.Empty; // "RECEIPT", "ISSUE", etc.
}

public record LotSerialValidationDto
{
    public bool IsValid { get; init; }
    public List<string> ValidationMessages { get; init; } = new();
    public bool LotRequired { get; init; }
    public bool SerialRequired { get; init; }
    public bool LotExists { get; init; }
    public bool SerialExists { get; init; }
}
