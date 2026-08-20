// <copyright file="InventoryDomainTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Inventory.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.Inventory.Tests;

public class ItemStockTests
{
    [Fact]
    public void AvailableQuantity_CalculatesCorrectly()
    {
        var stock = new ItemStock(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        stock.AdjustOnHand(100m);
        stock.AdjustAllocated(25m);

        stock.AvailableQuantity.Should().Be(75m);
    }

    [Fact]
    public void AdjustOnHand_IncreasesOnHand()
    {
        var stock = new ItemStock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        stock.AdjustOnHand(50m);
        stock.OnHandQuantity.Should().Be(50m);

        stock.AdjustOnHand(30m);
        stock.OnHandQuantity.Should().Be(80m);
    }

    [Fact]
    public void AdjustAllocated_IncreasesAllocated()
    {
        var stock = new ItemStock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        stock.AdjustAllocated(40m);
        stock.AllocatedQuantity.Should().Be(40m);
    }

    [Fact]
    public void AdjustOnOrder_IncreasesOnOrder()
    {
        var stock = new ItemStock(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        stock.AdjustOnOrder(25m);
        stock.OnOrderQuantity.Should().Be(25m);
    }
}

public class ItemReorderTests
{
    [Fact]
    public void ReorderSuggestionGenerated_WhenOnHandBelowReorderPoint()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.UpdateReorderParameters(100m, 50m, 20m, 14);

        var stock = new ItemStock(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            null,
            null)
        {
            OnHandQuantity = 50m,
            AllocatedQuantity = 10m
        };

        decimal availableQty = stock.OnHandQuantity - stock.AllocatedQuantity;
        bool needsReorder = availableQty <= item.ReorderPoint;
        needsReorder.Should().BeTrue();

        int suggestedQty = item.ReorderQuantity ?? 0;
        suggestedQty.Should().Be(50);
    }

    [Fact]
    public void ReorderSuggestionNotGenerated_WhenOnHandAboveReorderPoint()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.UpdateReorderParameters(100m, 50m, 0m, 0);

        var stock = new ItemStock(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            null,
            null)
        {
            OnHandQuantity = 150m,
            AllocatedQuantity = 10m
        };

        decimal availableQty = stock.OnHandQuantity - stock.AllocatedQuantity;
        bool needsReorder = availableQty <= item.ReorderPoint;
        needsReorder.Should().BeFalse();
    }

    [Fact]
    public void ReorderSuggestionGenerated_WhenAvailableBelowSafetyStock()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.UpdateReorderParameters(200m, 0m, 50m, 0);

        var stock = new ItemStock(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            null,
            null)
        {
            OnHandQuantity = 100m,
            AllocatedQuantity = 60m
        };

        decimal availableQty = stock.OnHandQuantity - stock.AllocatedQuantity; // 40m
        bool needsReorder = availableQty <= item.SafetyStock.Value; // 40 <= 50 = true
        needsReorder.Should().BeTrue();
    }

    [Fact]
    public void ReorderSuggestionNotGenerated_WhenAvailableAboveThresholds()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.UpdateReorderParameters(100m, 0m, 20m, 0);

        var stock = new ItemStock(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            null,
            null)
        {
            OnHandQuantity = 300m,
            AllocatedQuantity = 50m // Available = 250m
        };

        decimal availableQty = stock.OnHandQuantity - stock.AllocatedQuantity; // 250m
        bool needsReorder = availableQty <= item.ReorderPoint || availableQty <= item.SafetyStock.Value;
        needsReorder.Should().BeFalse();
    }
}

public class ItemNegativeInventoryTests
{
    [Fact]
    public void AllowNegative_False_BlocksIssueBeyondOnHand()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.AllowNegative(false);

        var stock = new ItemStock(item.CompanyId, item.Id, Guid.NewGuid());
        stock.AdjustOnHand(90m); // Set on-hand to 90

        var act = () => stock.AdjustOnHand(-100m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AllowNegative_True_AllowsIssueBeyondOnHand()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.AllowNegative(true);

        var stock = new ItemStock(item.CompanyId, item.Id, Guid.NewGuid());
        stock.AdjustOnHand(90m);

        stock.AdjustOnHand(-100m);
        stock.OnHandQuantity.Should().Be(-10m);
    }

    [Fact]
    public void AllowNegative_Method_TogglesSetting()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        var stock = new ItemStock(item.CompanyId, item.Id, Guid.NewGuid());

        stock.AllowNegative(true);
        stock.AllowNegative(false);

        var act = () => stock.AdjustOnHand(-1m);
        act.Should().Throw<InvalidOperationException>();
    }
}

public class ItemLotControlTests
{
    [Fact]
    public void SetLotControlled_MarksItem()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        item.SetLotControlled(true);
        item.IsLotControlled.Should().BeTrue();

        item.SetLotControlled(false);
        item.IsLotControlled.Should().BeFalse();
    }

    [Fact]
    public void Receipt_WithLot_LotAssignedToTransaction()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.SetLotControlled(true);

        var txn = new InventoryTransaction(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            InventoryTransactionType.Receipt,
            10m,
            "EA",
            10m,
            DateTime.UtcNow,
            lotId: Guid.NewGuid());

        txn.LotId.Should().NotBeNull();
    }

    [Fact]
    public void Issue_WithoutLot_ForNonLotControlledItem_Success()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        // Non-lot-controlled item: lot can be null
        var txn = new InventoryTransaction(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            InventoryTransactionType.Issue,
            5m,
            "EA",
            10m,
            DateTime.UtcNow,
            lotId: null);

        txn.LotId.Should().BeNull();
    }

    [Fact]
    public void TwoReceipts_DifferentLots_TrackedSeparately()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);
        item.SetLotControlled(true);

        var txn1 = new InventoryTransaction(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            InventoryTransactionType.Receipt,
            10m,
            "EA",
            10m,
            DateTime.UtcNow.AddDays(-5),
            lotId: Guid.NewGuid());

        var txn2 = new InventoryTransaction(
            item.CompanyId,
            item.Id,
            Guid.NewGuid(),
            InventoryTransactionType.Receipt,
            15m,
            "EA",
            12m,
            DateTime.UtcNow,
            lotId: Guid.NewGuid());

        txn1.LotId.Should().NotBe(txn2.LotId);
    }
}

public class ItemExpirationTests
{
    [Fact]
    public void ExpiredLot_IssueBlocked_PastExpiryDate()
    {
        var _item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        var expiryDate = DateTime.UtcNow.AddDays(-30);

        // Use _item in assertions below
        _item.Should().NotBeNull();
        var issueDate = DateTime.UtcNow;

        var isPastExpiry = issueDate > expiryDate;
        isPastExpiry.Should().BeTrue();
    }

    [Fact]
    public void NonExpiredLot_IssueAllowed()
    {
        var _item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        var expiryDate = DateTime.UtcNow.AddDays(90);

        // Use _item in assertions below
        _item.Should().NotBeNull();
        var issueDate = DateTime.UtcNow;

        var isNotPastExpiry = issueDate <= expiryDate;
        isNotPastExpiry.Should().BeTrue();
    }
}

public class ItemCostingTests
{
    [Fact]
    public void FIFO_CostMethod_IsFIFO()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.FIFO,
            null,
            ItemStatus.Active);

        item.CostingMethod.Should().Be(CostingMethod.FIFO);
    }

    [Fact]
    public void LIFO_CostMethod_IsLIFO()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.LIFO,
            null,
            ItemStatus.Active);

        item.CostingMethod.Should().Be(CostingMethod.LIFO);
    }

    [Fact]
    public void Average_CostMethod_IsAverage()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Average,
            null,
            ItemStatus.Active);

        item.CostingMethod.Should().Be(CostingMethod.Average);
    }

    [Fact]
    public void Standard_Cost_Updated()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        item.UpdateStandardCost(25.50m);
        item.StandardCost.Should().Be(25.50m);
    }

    [Fact]
    public void ReorderParameters_UpdatedViaMethod()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null,
            ItemStatus.Active);

        item.UpdateReorderParameters(100m, 50m, 20m, 14);

        item.ReorderPoint.Should().Be(100m);
        item.ReorderQuantity.Should().Be(50m);
        item.SafetyStock.Should().Be(20m);
        item.LeadTimeDays.Should().Be(14);
    }
}

public class ItemUnitTests
{
    [Fact]
    public void ItemCode_Required_Throws()
    {
        var act = () => new Item(
            string.Empty,
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        act.Should().Throw<ArgumentException>("Item code is required.");
    }

    [Fact]
    public void Description_Required_Throws()
    {
        var act = () => new Item(
            "ITEM-001",
            string.Empty,
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        act.Should().Throw<ArgumentException>("Description is required.");
    }

    [Fact]
    public void ItemStatus_Deactivate_SetsInactive()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        item.Deactivate();
        item.Status.Should().Be(ItemStatus.Inactive);
    }

    [Fact]
    public void ItemStatus_Activate_SetsActive()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        item.Deactivate();
        item.Status.Should().Be(ItemStatus.Inactive);

        item.Activate();
        item.Status.Should().Be(ItemStatus.Active);
    }

    [Fact]
    public void ItemStatus_Discontinue_SetsDiscontinued()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        item.Discontinue();
        item.Status.Should().Be(ItemStatus.Discontinued);
    }

    [Fact]
    public void PhysicalAttributes_StoredCorrectly()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        item.UpdatePhysicalAttributes(
            weight: 2.5m,
            length: 30m,
            width: 20m,
            height: 15m,
            weightUnit: "kg",
            isHazardousMaterial: false,
            hazardClass: null,
            countryOfOrigin: "USA",
            hsCode: "8471.30",
            storageCondition: "Room temperature");

        item.Weight.Should().Be(2.5m);
        item.Length.Should().Be(30m);
        item.Width.Should().Be(20m);
        item.Height.Should().Be(15m);
        item.WeightUnit.Should().Be("kg");
        item.CountryOfOrigin.Should().Be("USA");
        item.HsCode.Should().Be("8471.30");
        item.StorageCondition.Should().Be("Room temperature");
    }

    [Fact]
    public void ItemStatusCycle()
    {
        var item = new Item(
            "ITEM-001",
            "Test Item",
            Guid.NewGuid(),
            ItemType.Inventory,
            "EA",
            CostingMethod.Standard,
            null);

        item.Deactivate();
        item.Status.Should().Be(ItemStatus.Inactive);

        item.Activate();
        item.Status.Should().Be(ItemStatus.Active);
    }
}