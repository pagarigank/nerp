// <copyright file="EInvoiceComplianceHelper.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Infrastructure;

public static class EInvoiceComplianceHelper
{
    /// <summary>
    /// RM10,000 individual invoice rule - invoices >= this amount must be submitted individually.
    /// </summary>
    public const decimal IndividualInvoiceThreshold = 10000m;

    /// <summary>
    /// 72-hour cancellation window in hours.
    /// </summary>
    public const int CancellationWindowHours = 72;

    /// <summary>
    /// Check if a document can be cancelled within the 72-hour window.
    /// Returns true if cancellation is allowed.
    /// </summary>
    public static bool CanCancelDocument(DateTime submittedAt, DateTime now)
    {
        var windowEnd = submittedAt.AddHours(CancellationWindowHours);
        return now <= windowEnd;
    }

    /// <summary>
    /// Check if an invoice amount requires individual submission (not consolidatable).
    /// RM10,000 rule: invoices >= threshold must be invoiced individually.
    /// </summary>
    public static bool RequiresIndividualSubmission(decimal amount)
    {
        return amount >= IndividualInvoiceThreshold;
    }

    /// <summary>
    /// Calculate the cancellation deadline for a document.
    /// </summary>
    public static DateTime CalculateCancellationDeadline(DateTime submittedAt)
    {
        return submittedAt.AddHours(CancellationWindowHours);
    }

    /// <summary>
    /// Get the time remaining until cancellation window expires.
    /// Returns negative if window has expired.
    /// </summary>
    public static TimeSpan GetTimeRemainingInCancellationWindow(DateTime submittedAt, DateTime now)
    {
        var deadline = CalculateCancellationDeadline(submittedAt);
        return deadline - now;
    }
}