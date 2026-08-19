// <copyright file="ThreeWayMatchService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class ThreeWayMatchService : IThreeWayMatchService
{
    private const decimal DefaultTolerancePercent = 0.05m;

    public Task<ThreeWayMatchResult> ValidateMatchAsync(
        ThreeWayMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var warnings = new List<string>();
        var totalVariance = 0m;
        var hasQuantityVariance = false;
        var hasPriceVariance = false;

        foreach (var line in request.Lines)
        {
            var poQty = line.OrderedQuantity;
            var receiptQty = line.ReceivedQuantity;
            var invoiceQty = line.InvoicedQuantity;

            if (poQty <= 0)
            {
                errors.Add($"Line item '{line.ItemCode}': Purchase Order quantity must be positive.");
                continue;
            }

            if (invoiceQty > receiptQty)
            {
                hasQuantityVariance = true;
                var overReceiptPct = (invoiceQty - receiptQty) / receiptQty;
                if (overReceiptPct > line.TolerancePercent)
                {
                    errors.Add($"Line item '{line.ItemCode}': Invoice quantity ({invoiceQty}) exceeds received quantity ({receiptQty}) by more than tolerance ({line.TolerancePercent:P1}).");
                }
                else
                {
                    warnings.Add($"Line item '{line.ItemCode}': Invoice quantity ({invoiceQty}) slightly exceeds received quantity ({receiptQty}). Within tolerance.");
                }
            }

            if (invoiceQty > poQty)
            {
                hasQuantityVariance = true;
                var overOrderPct = (invoiceQty - poQty) / poQty;
                if (overOrderPct > line.TolerancePercent)
                {
                    errors.Add($"Line item '{line.ItemCode}': Invoice quantity ({invoiceQty}) exceeds ordered quantity ({poQty}) by more than tolerance ({line.TolerancePercent:P1}).");
                }
            }

            var expectedAmount = invoiceQty * line.UnitPrice;
            var variance = Math.Abs(line.ExtendedAmount - expectedAmount);
            if (variance > 0.01m)
            {
                hasPriceVariance = true;
                var variancePct = variance / expectedAmount;
                if (variancePct > line.TolerancePercent)
                {
                    errors.Add($"Line item '{line.ItemCode}': Price variance of {variance:C2} ({variancePct:P1}) exceeds tolerance ({line.TolerancePercent:P1}).");
                }
                else
                {
                    warnings.Add($"Line item '{line.ItemCode}': Price variance of {variance:C2} is within tolerance.");
                }

                totalVariance += variance;
            }
        }

        var tolerance = request.Lines.Count > 0
            ? request.Lines.Max(l => l.TolerancePercent)
            : DefaultTolerancePercent;

        var result = new ThreeWayMatchResult(
            errors.Count == 0,
            hasQuantityVariance,
            hasPriceVariance,
            totalVariance,
            tolerance,
            warnings,
            errors);

        return Task.FromResult(result);
    }
}
