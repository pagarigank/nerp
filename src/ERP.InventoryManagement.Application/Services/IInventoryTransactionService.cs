// <copyright file="IInventoryTransactionService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.InventoryManagement.Application.Services;

using ERP.InventoryManagement.Application.DTOs.Transactions;
using ERP.Shared.Kernel.Api;

/// <summary>
/// Service for inventory transaction operations.
/// </summary>
public interface IInventoryTransactionService
{
    /// <summary>
    /// Creates a new inventory receipt transaction.
    /// </summary>
    Task<ApiResponse<InventoryTransactionDto>> CreateReceiptAsync(CreateReceiptDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inventory issue transaction.
    /// </summary>
    Task<ApiResponse<InventoryTransactionDto>> CreateIssueAsync(CreateIssueDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inventory adjustment transaction.
    /// </summary>
    Task<ApiResponse<InventoryTransactionDto>> CreateAdjustmentAsync(CreateAdjustmentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new inventory transfer transaction.
    /// </summary>
    Task<ApiResponse<InventoryTransactionDto>> CreateTransferAsync(CreateTransferDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes physical count and creates adjustments.
    /// </summary>
    Task<ApiResponse<PhysicalCountResultDto>> ProcessPhysicalCountAsync(ProcessPhysicalCountDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates lot/serial number requirements before transaction.
    /// </summary>
    Task<ApiResponse<LotSerialValidationDto>> ValidateLotSerialAsync(ValidateLotSerialDto dto, CancellationToken cancellationToken = default);
}
