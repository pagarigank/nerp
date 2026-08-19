// <copyright file="IThreeWayMatchService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsPayable.Infrastructure;

public interface IThreeWayMatchService
{
    Task<ThreeWayMatchResult> ValidateMatchAsync(
        ThreeWayMatchRequest request,
        CancellationToken cancellationToken = default);
}

public record ThreeWayMatchLine(
    Guid? PurchaseOrderLineId,
    string ItemCode,
    string Description,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal InvoicedQuantity,
    decimal UnitPrice,
    decimal ExtendedAmount,
    decimal TolerancePercent);

public record ThreeWayMatchRequest(
    Guid CompanyId,
    Guid VendorId,
    string InvoiceNumber,
    IReadOnlyList<ThreeWayMatchLine> Lines,
    decimal InvoiceTotal);

public record ThreeWayMatchResult(
    bool IsValid,
    bool HasQuantityVariance,
    bool HasPriceVariance,
    decimal TotalVarianceAmount,
    decimal TolerancePercent,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
