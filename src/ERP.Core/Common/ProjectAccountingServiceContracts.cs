// <copyright file="ProjectAccountingServiceContracts.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Core.Common;

/// <summary>
/// Shared contract for project-cost validation, exposed from ERP.Core so the
/// Payroll module can validate that a labor/expense line targets an open
/// project/task with an available budget line (spec §5.12) without taking a
/// compile-time dependency on the Project Accounting module. Project Accounting
/// implements this against its own DbContext. Mirrors the existing
/// <see cref="ICreditLimitCheck"/> / <see cref="IInventoryAvailability"/> pattern.
/// </summary>
public interface IProjectCostValidation
{
    Task<ProjectCostValidationResult> ValidateAsync(
        Guid companyId,
        Guid? projectId,
        Guid? taskId,
        decimal proposedAmount,
        CancellationToken cancellationToken = default);
}

public record ProjectCostValidationResult(
    bool IsValid,
    string? Message,
    decimal BudgetRemaining,
    bool ProjectIsOpen,
    bool TaskIsOpen);
