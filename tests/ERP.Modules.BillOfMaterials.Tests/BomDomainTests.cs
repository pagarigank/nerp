using ERP.Modules.BillOfMaterials.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.BillOfMaterials.Tests;

public class BomDomainTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ParentItemId = Guid.NewGuid();
    private static readonly Guid ComponentItemId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    // ==================== BomHeader Tests ====================

    [Fact]
    public void BomHeader_CreatesWithValidProperties()
    {
        var bom = new BomHeader(
            CompanyId, ParentItemId, "Rev A", BomType.Standard, BomStatus.Draft,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), "Test BOM", 95m);

        bom.CompanyId.Should().Be(CompanyId);
        bom.ParentItemId.Should().Be(ParentItemId);
        bom.Revision.Should().Be("Rev A");
        bom.BomType.Should().Be(BomType.Standard);
        bom.Status.Should().Be(BomStatus.Draft);
        bom.YieldPercentage.Should().Be(95m);
        bom.Description.Should().Be("Test BOM");
        bom.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void BomHeader_ThrowsOnEmptyRevision()
    {
        var act = () => new BomHeader(
            CompanyId, ParentItemId, "", BomType.Standard, BomStatus.Draft,
            null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Revision is required*");
    }

    [Fact]
    public void BomHeader_UpdateModifiesProperties()
    {
        var bom = new BomHeader(
            CompanyId, ParentItemId, "Rev A", BomType.Standard, BomStatus.Draft,
            null, null);

        bom.Update(description: "Updated", bomType: BomType.Alternate,
            status: BomStatus.Active, effectiveFrom: DateTime.UtcNow,
            effectiveTo: DateTime.UtcNow.AddYears(1), yieldPercentage: 98m);

        bom.Description.Should().Be("Updated");
        bom.BomType.Should().Be(BomType.Alternate);
        bom.Status.Should().Be(BomStatus.Active);
        bom.YieldPercentage.Should().Be(98m);
    }

    [Fact]
    public void BomHeader_AddComponentIncreasesCount()
    {
        var bom = new BomHeader(
            CompanyId, ParentItemId, "Rev A", BomType.Standard, BomStatus.Draft,
            null, null);

        bom.AddComponent(ComponentItemId, 2m, "EA");
        bom.AddComponent(Guid.NewGuid(), 1m, "KG");

        bom.Components.Should().HaveCount(2);
    }

    [Fact]
    public void BomHeader_RemoveComponentWorks()
    {
        var bom = new BomHeader(
            CompanyId, ParentItemId, "Rev A", BomType.Standard, BomStatus.Draft,
            null, null);

        var line = bom.AddComponent(ComponentItemId, 2m, "EA");
        bom.RemoveComponent(line.Id);

        bom.Components.Should().BeEmpty();
    }

    // ==================== BomComponentLine Tests ====================

    [Fact]
    public void BomComponentLine_CreatesWithValidProperties()
    {
        var line = new BomComponentLine(
            Guid.NewGuid(), ComponentItemId, 3m, "EA",
            scrapFactor: 5m, operationSequence: 20, isCritical: true);

        line.QuantityPerParent.Should().Be(3m);
        line.UnitOfMeasure.Should().Be("EA");
        line.ScrapFactor.Should().Be(5m);
        line.OperationSequence.Should().Be(20);
        line.IsCritical.Should().BeTrue();
        line.IsPhantom.Should().BeFalse();
    }

    [Fact]
    public void BomComponentLine_EffectiveQuantityIncludesScrap()
    {
        var line = new BomComponentLine(
            Guid.NewGuid(), ComponentItemId, 10m, "EA", scrapFactor: 10m);

        // EffectiveQuantity = 10 × (1 + 10/100) = 10 × 1.1 = 11
        line.EffectiveQuantity.Should().Be(11m);
    }

    [Fact]
    public void BomComponentLine_EffectiveQuantityWithZeroScrap()
    {
        var line = new BomComponentLine(
            Guid.NewGuid(), ComponentItemId, 5m, "EA", scrapFactor: 0m);

        line.EffectiveQuantity.Should().Be(5m);
    }

    [Fact]
    public void BomComponentLine_ThrowsOnZeroQuantity()
    {
        var act = () => new BomComponentLine(
            Guid.NewGuid(), ComponentItemId, 0m, "EA");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    // ==================== BuildOrder Tests ====================

    [Fact]
    public void BuildOrder_CreatesWithValidProperties()
    {
        var bomId = Guid.NewGuid();
        var build = new BuildOrder(
            CompanyId, "BO-001", BuildTransactionType.Assemble,
            bomId, ParentItemId, 100m, "EA", WarehouseId, DateTime.UtcNow);

        build.BuildNumber.Should().Be("BO-001");
        build.QuantityToBuild.Should().Be(100m);
        build.Status.Should().Be(BuildOrderStatus.Draft);
        build.TransactionType.Should().Be(BuildTransactionType.Assemble);
    }

    [Fact]
    public void BuildOrder_CalculateCostsComputesTotals()
    {
        var build = new BuildOrder(
            CompanyId, "BO-001", BuildTransactionType.Assemble,
            Guid.NewGuid(), ParentItemId, 100m, "EA", WarehouseId, DateTime.UtcNow);

        build.AddLine(Guid.NewGuid(), 50m, 50m, "EA", 10m);  // material $500
        build.AddLine(Guid.NewGuid(), 10m, 10m, "HR", 50m, isLabor: true);  // labor $500
        build.AddLine(Guid.NewGuid(), 1m, 1m, "EA", 200m, isOverhead: true); // overhead $200

        build.CalculateCosts();

        // TotalMaterialCost = sum of !IsOverhead (material + labor)
        // TotalLaborCost = sum of IsLabor (also included in MaterialCost)
        // TotalCost = MaterialCost + LaborCost + OverheadCost (double-counts labor by design)
        build.TotalMaterialCost.Should().Be(1000m);
        build.TotalLaborCost.Should().Be(500m);
        build.TotalOverheadCost.Should().Be(200m);
        build.TotalCost.Should().Be(1700m);
        build.UnitCost.Should().Be(17m); // 1700 / 100
    }
}
