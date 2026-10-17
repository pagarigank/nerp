// <copyright file="SalespersonTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.OrderManagement.Domain.Entities;
using Xunit;

namespace ERP.Modules.OrderManagement.Tests;

public class SalespersonTests
{
    [Fact]
    public void SalespersonRoute_Create_SetsPropertiesCorrectly()
    {
        var companyId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var route = new SalespersonRoute(
            companyId,
            repId,
            customerId,
            DayOfWeek.Monday,
            1,
            "09:30 AM",
            "Front desk contact");

        Assert.Equal(companyId, route.CompanyId);
        Assert.Equal(repId, route.SalesRepId);
        Assert.Equal(customerId, route.CustomerId);
        Assert.Equal(DayOfWeek.Monday, route.DayOfWeek);
        Assert.Equal(1, route.SequenceOrder);
        Assert.Equal("09:30 AM", route.EstimatedTime);
        Assert.Equal("Front desk contact", route.Notes);
        Assert.True(route.IsActive);
    }

    [Fact]
    public void SalespersonRoute_Update_ModifiesProperties()
    {
        var route = new SalespersonRoute(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DayOfWeek.Monday,
            1);

        route.Update(DayOfWeek.Wednesday, 3, "02:00 PM", "Side door delivery", false);

        Assert.Equal(DayOfWeek.Wednesday, route.DayOfWeek);
        Assert.Equal(3, route.SequenceOrder);
        Assert.Equal("02:00 PM", route.EstimatedTime);
        Assert.Equal("Side door delivery", route.Notes);
        Assert.False(route.IsActive);
    }

    [Fact]
    public void SalesCall_AddLine_CalculatesLineTotalAndGrandTotal()
    {
        var call = new SalesCall(
            Guid.NewGuid(),
            "CALL-20261017-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            DateTime.UtcNow.Date,
            DateTime.UtcNow,
            "Customer visit",
            isOfflineCreated: true,
            clientCallId: "client-uuid-123");

        Assert.Equal(SalesCallStatus.InProgress, call.Status);
        Assert.Equal(0m, call.TotalAmount);
        Assert.True(call.IsOfflineCreated);
        Assert.Equal("client-uuid-123", call.ClientCallId);

        // Add Line 1: 5 units @ $20 = $100
        var line1 = new SalesCallLine(
            call.Id,
            1,
            Guid.NewGuid(),
            "ITEM-001",
            "Standard Widget",
            5m,
            20m,
            "EA");

        call.AddLine(line1);

        Assert.Equal(100m, line1.LineTotal);
        Assert.Equal(100m, call.TotalAmount);

        // Add Line 2: 10 units @ $50 with 10% discount = $450
        var line2 = new SalesCallLine(
            call.Id,
            2,
            Guid.NewGuid(),
            "ITEM-002",
            "Premium Widget",
            10m,
            50m,
            "BOX",
            discountPercent: 10m);

        call.AddLine(line2);

        Assert.Equal(450m, line2.LineTotal);
        Assert.Equal(550m, call.TotalAmount);
    }

    [Fact]
    public void SalesCall_CompleteCall_MarksCompletedAndLinksOrder()
    {
        var call = new SalesCall(
            Guid.NewGuid(),
            "CALL-20261017-002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            DateTime.UtcNow.Date,
            DateTime.UtcNow);

        var endTime = DateTime.UtcNow.AddMinutes(15);
        call.CompleteCall(endTime, "Closed sale");

        Assert.Equal(SalesCallStatus.Completed, call.Status);
        Assert.Equal(endTime, call.EndTime);
        Assert.Equal("Closed sale", call.Notes);

        var orderId = Guid.NewGuid();
        call.LinkSalesOrder(orderId);
        Assert.Equal(orderId, call.SalesOrderId);

        var syncTime = DateTime.UtcNow;
        call.MarkSynced(syncTime);
        Assert.Equal(syncTime, call.SyncedAt);
    }

    [Fact]
    public void SalesCall_CancelCall_MarksCancelledWithReason()
    {
        var call = new SalesCall(
            Guid.NewGuid(),
            "CALL-20261017-003",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            DateTime.UtcNow.Date,
            DateTime.UtcNow,
            "Scheduled meeting");

        call.CancelCall("Store closed on arrival");

        Assert.Equal(SalesCallStatus.Cancelled, call.Status);
        Assert.NotNull(call.EndTime);
        Assert.Contains("Cancelled: Store closed on arrival", call.Notes, StringComparison.Ordinal);
    }
}
