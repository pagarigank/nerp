// <copyright file="EInvoiceComplianceHelperTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.EInvoice.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.MalaysianTax.Tests;

public class EInvoiceComplianceHelperTests
{
    // ===== CanCancelDocument Tests =====

    [Fact]
    public void CanCancelDocument_WithinWindow_ReturnsTrue()
    {
        var submittedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 16, 8, 0, 0, DateTimeKind.Utc);

        var result = EInvoiceComplianceHelper.CanCancelDocument(submittedAt, now);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanCancelDocument_AtExactDeadline_ReturnsTrue()
    {
        var submittedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 18, 10, 0, 0, DateTimeKind.Utc);

        var result = EInvoiceComplianceHelper.CanCancelDocument(submittedAt, now);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanCancelDocument_AfterWindow_ReturnsFalse()
    {
        var submittedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 18, 10, 0, 1, DateTimeKind.Utc);

        var result = EInvoiceComplianceHelper.CanCancelDocument(submittedAt, now);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanCancelDocument_JustSubmitted_ReturnsTrue()
    {
        var submittedAt = new DateTime(2024, 6, 18, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 18, 10, 0, 30, DateTimeKind.Utc);

        var result = EInvoiceComplianceHelper.CanCancelDocument(submittedAt, now);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanCancelDocument_FarPastDeadline_ReturnsFalse()
    {
        var submittedAt = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 18, 0, 0, 0, DateTimeKind.Utc);

        var result = EInvoiceComplianceHelper.CanCancelDocument(submittedAt, now);

        result.Should().BeFalse();
    }

    // ===== RequiresIndividualSubmission Tests =====

    [Fact]
    public void RequiresIndividualSubmission_BelowThreshold_ReturnsFalse()
    {
        var result = EInvoiceComplianceHelper.RequiresIndividualSubmission(9999.99m);

        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresIndividualSubmission_AtThreshold_ReturnsTrue()
    {
        var result = EInvoiceComplianceHelper.RequiresIndividualSubmission(10000m);

        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresIndividualSubmission_AboveThreshold_ReturnsTrue()
    {
        var result = EInvoiceComplianceHelper.RequiresIndividualSubmission(50000m);

        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresIndividualSubmission_ZeroAmount_ReturnsFalse()
    {
        var result = EInvoiceComplianceHelper.RequiresIndividualSubmission(0m);

        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresIndividualSubmission_NegativeAmount_ReturnsFalse()
    {
        var result = EInvoiceComplianceHelper.RequiresIndividualSubmission(-100m);

        result.Should().BeFalse();
    }

    // ===== CalculateCancellationDeadline Tests =====

    [Fact]
    public void CalculateCancellationDeadline_Returns72HoursFromSubmission()
    {
        var submittedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var deadline = EInvoiceComplianceHelper.CalculateCancellationDeadline(submittedAt);

        deadline.Should().Be(new DateTime(2024, 6, 18, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateCancellationDeadline_CrossesDayBoundary()
    {
        var submittedAt = new DateTime(2024, 6, 17, 23, 0, 0, DateTimeKind.Utc);

        var deadline = EInvoiceComplianceHelper.CalculateCancellationDeadline(submittedAt);

        deadline.Should().Be(new DateTime(2024, 6, 20, 23, 0, 0, DateTimeKind.Utc));
    }

    // ===== GetTimeRemainingInCancellationWindow Tests =====

    [Fact]
    public void GetTimeRemaining_JustSubmitted_ShowsPositiveTimeRemaining()
    {
        var submittedAt = new DateTime(2024, 6, 18, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 18, 11, 0, 0, DateTimeKind.Utc);

        var remaining = EInvoiceComplianceHelper.GetTimeRemainingInCancellationWindow(submittedAt, now);

        remaining.Should().BeGreaterThan(TimeSpan.Zero);
        remaining.TotalHours.Should().BeApproximately(71.0, 0.01);
    }

    [Fact]
    public void GetTimeRemaining_AfterDeadline_ShowsNegativeTimeRemaining()
    {
        var submittedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 19, 10, 0, 0, DateTimeKind.Utc);

        var remaining = EInvoiceComplianceHelper.GetTimeRemainingInCancellationWindow(submittedAt, now);

        remaining.Should().BeLessThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetTimeRemaining_AtDeadline_ShowsZero()
    {
        var submittedAt = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2024, 6, 18, 10, 0, 0, DateTimeKind.Utc);

        var remaining = EInvoiceComplianceHelper.GetTimeRemainingInCancellationWindow(submittedAt, now);

        remaining.Should().Be(TimeSpan.Zero);
    }

    // ===== Constants Tests =====

    [Fact]
    public void IndividualInvoiceThreshold_Is10000()
    {
        EInvoiceComplianceHelper.IndividualInvoiceThreshold.Should().Be(10000m);
    }

    [Fact]
    public void CancellationWindowHours_Is72()
    {
        EInvoiceComplianceHelper.CancellationWindowHours.Should().Be(72);
    }
}
