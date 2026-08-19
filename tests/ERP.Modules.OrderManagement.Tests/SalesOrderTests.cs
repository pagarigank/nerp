// <copyright file="SalesOrderTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using ERP.Modules.OrderManagement.Domain.Entities;
using Xunit;

namespace ERP.Modules.OrderManagement.Tests;

public class SalesOrderTests
{
    private static SalesOrder CreateDraftOrder(decimal discountPercent = 0m)
    {
        var order = new SalesOrder(
            "SO-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            salesRepId: "REP-1");

        order.AddLine(new SalesOrderLine(
            order.Id,
            1,
            Guid.NewGuid(),
            "Widget",
            10m,
            100m,
            "EA",
            discountPercent,
            0m,
            Guid.NewGuid()));
        return order;
    }

    [Fact]
    public void AddLine_WithDiscountOverThreshold_SetsRequiresDiscountApproval()
    {
        var order = CreateDraftOrder(discountPercent: 25m);
        Assert.True(order.RequiresDiscountApproval);
        Assert.False(order.DiscountApproved);
    }

    [Fact]
    public void AddLine_WithDiscountUnderThreshold_DoesNotRequireApproval()
    {
        var order = CreateDraftOrder(discountPercent: 15m);
        Assert.False(order.RequiresDiscountApproval);
    }

    [Fact]
    public void Confirm_WithUnapprovedDiscount_Throws()
    {
        var order = CreateDraftOrder(discountPercent: 25m);
        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void MarkDiscountApproved_ThenConfirm_Succeeds()
    {
        var order = CreateDraftOrder(discountPercent: 25m);
        order.MarkDiscountApproved("manager@acme.com");
        Assert.True(order.DiscountApproved);

        var ex = Record.Exception(() => order.Confirm());
        Assert.Null(ex);
    }

    [Fact]
    public void MultiplePartialShipments_AccumulateAndTrackRemaining()
    {
        var order = CreateDraftOrder();
        order.Confirm();

        var line = order.Lines.First();
        order.MarkShipped(line.Id, 6m);
        Assert.Equal(6m, line.ShippedQuantity);
        Assert.Equal(4m, order.RemainingToShip);

        order.MarkShipped(line.Id, 4m);
        Assert.Equal(10m, line.ShippedQuantity);
        Assert.Equal(0m, order.RemainingToShip);
    }

    [Fact]
    public void ShipmentOverOrderedQuantity_Throws()
    {
        var order = CreateDraftOrder();
        order.Confirm();
        var line = order.Lines.First();
        Assert.Throws<InvalidOperationException>(() => order.MarkShipped(line.Id, 11m));
    }
}
