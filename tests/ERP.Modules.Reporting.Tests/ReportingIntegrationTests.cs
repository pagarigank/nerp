// <copyright file="ReportingIntegrationTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Reporting.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.Reporting.Tests;

public class ReportingIntegrationTests : IDisposable
{
    private readonly ReportingDbContext _db;

    public ReportingIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ReportingDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task ReportDefinition_CRUD_FullLifecycle()
    {
        var companyId = Guid.NewGuid();
        var report = new ReportDefinition(
            companyId,
            "Test Report",
            "GL",
            "Test",
            "Test report for integration",
            "Standard",
            "dbo.TestView",
            "SELECT * FROM TestView",
            "{\"CompanyId\":{\"type\":\"guid\",\"required\":true}}",
            null);

        _db.ReportDefinitions.Add(report);
        await _db.SaveChangesAsync();

        var found = await _db.ReportDefinitions.FindAsync(report.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Test Report");

        found.Update("Updated Report", "AP", "Updated", "Updated description", "Custom", "dbo.NewView", "SELECT 1", null, null);
        await _db.SaveChangesAsync();

        var updated = await _db.ReportDefinitions.FindAsync(report.Id);
        updated!.Name.Should().Be("Updated Report");
        updated.Module.Should().Be("AP");

        found.MarkDeleted("test-user");
        await _db.SaveChangesAsync();

        var deleted = await _db.ReportDefinitions.FindAsync(report.Id);
        deleted!.DeletedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task ReportCategory_TreeHierarchy_BuildsCorrectly()
    {
        var companyId = Guid.NewGuid();

        var root = new ReportCategory(companyId, "Financial", null, 1, null, null);
        var child1 = new ReportCategory(companyId, "Balance Sheet", root.Id.ToString(), 1, null, null);
        var child2 = new ReportCategory(companyId, "Income Statement", root.Id.ToString(), 2, null, null);
        var grandchild = new ReportCategory(companyId, "Detailed BS", child1.Id.ToString(), 1, null, null);

        _db.ReportCategories.AddRange(root, child1, child2, grandchild);
        await _db.SaveChangesAsync();

        var all = await _db.ReportCategories
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        all.Count.Should().Be(4);

        var tree = BuildCategoryTree(all, null);
        tree.Count.Should().Be(1);
        tree[0].Name.Should().Be("Financial");
        tree[0].Children.Count.Should().Be(2);
        tree[0].Children[0].Name.Should().Be("Balance Sheet");
        tree[0].Children[0].Children.Count.Should().Be(1);
        tree[0].Children[0].Children[0].Name.Should().Be("Detailed BS");
        tree[0].Children[1].Name.Should().Be("Income Statement");
    }

    [Fact]
    public async Task ReportParameterSet_DefaultManagement_OnlyOneDefault()
    {
        var companyId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        var set1 = new ReportParameterSet(companyId, reportId, "Default", "{}", true, null);
        var set2 = new ReportParameterSet(companyId, reportId, "Monthly", "{\"period\":\"monthly\"}", false, null);
        var set3 = new ReportParameterSet(companyId, reportId, "Quarterly", "{\"period\":\"quarterly\"}", false, null);

        _db.ReportParameterSets.AddRange(set1, set2, set3);
        await _db.SaveChangesAsync();

        set1.SetDefault(false);
        set2.SetDefault(true);
        await _db.SaveChangesAsync();

        var sets = await _db.ReportParameterSets
            .Where(x => x.ReportDefinitionId == reportId)
            .ToListAsync();

        sets.Count(x => x.IsDefault).Should().Be(1);
        sets.First(x => x.IsDefault).Name.Should().Be("Monthly");
    }

    [Fact]
    public async Task ReportSubscription_ActivateDeactivate_Lifecycle()
    {
        var subscription = new ReportSubscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Weekly AP Aging",
            null,
            "PDF",
            "Weekly",
            "{\"day\":\"Monday\",\"time\":\"06:00\"}",
            "[\"admin@erp.com\"]");

        _db.ReportSubscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        subscription.Activate();
        await _db.SaveChangesAsync();

        var active = await _db.ReportSubscriptions.FindAsync(subscription.Id);
        active!.Status.Should().Be("Active");

        subscription.Deactivate();
        await _db.SaveChangesAsync();

        var deactivated = await _db.ReportSubscriptions.FindAsync(subscription.Id);
        deactivated!.Status.Should().Be("Inactive");

        subscription.RecordRun("Success");
        await _db.SaveChangesAsync();

        var ran = await _db.ReportSubscriptions.FindAsync(subscription.Id);
        ran!.LastRunStatus.Should().Be("Success");
        ran.LastRunOn.Should().NotBeNull();
        ran.RunCount.Should().Be(1);
    }

    [Fact]
    public async Task QuickQuery_CreateAndRun_Lifecycle()
    {
        var query = new QuickQuery(
            Guid.NewGuid(),
            "Open POs by Vendor",
            "PurchaseOrders",
            "{\"Status\":\"Open\"}",
            "[\"PoNumber\",\"VendorName\"]",
            "[\"PoNumber\",\"VendorName\",\"TotalAmount\"]",
            false,
            "admin@erp.com");

        _db.QuickQueries.Add(query);
        await _db.SaveChangesAsync();

        query.RecordRun();
        await _db.SaveChangesAsync();

        var found = await _db.QuickQueries.FindAsync(query.Id);
        found!.RunCount.Should().Be(1);
        found.LastRunOn.Should().NotBeNull();

        query.RecordRun();
        await _db.SaveChangesAsync();

        var afterSecondRun = await _db.QuickQueries.FindAsync(query.Id);
        afterSecondRun!.RunCount.Should().Be(2);
    }

    [Fact]
    public async Task DashboardWidget_PositionManagement_Works()
    {
        var companyId = Guid.NewGuid();
        var widget = new DashboardWidget(
            companyId,
            "exec-dashboard",
            "Cash Position",
            "StatCard",
            "CashPosition",
            null,
            null,
            0,
            0,
            6,
            4);

        _db.DashboardWidgets.Add(widget);
        await _db.SaveChangesAsync();

        widget.Update("Cash Position Updated", "StatCard", "CashPosition", null, null, 1, 2, 6, 3);
        await _db.SaveChangesAsync();

        var found = await _db.DashboardWidgets.FindAsync(widget.Id);
        found!.PositionX.Should().Be(1);
        found.PositionY.Should().Be(2);
        found.Width.Should().Be(6);
        found.Height.Should().Be(3);
    }

    [Fact]
    public async Task ReportUsageLog_TracksMultipleExecutions()
    {
        var companyId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        var logs = new List<ReportUsageLog>();
        for (int i = 0; i < 10; i++)
        {
            logs.Add(new ReportUsageLog(
                companyId,
                "Standard",
                reportId,
                null,
                $"user{i}@erp.com",
                $"{{\"run\":{i}}}",
                i % 2 == 0 ? "CSV" : "PDF",
                1000 + (i * 100),
                100 + (i * 10)));
        }

        _db.ReportUsageLogs.AddRange(logs);
        await _db.SaveChangesAsync();

        var query = _db.ReportUsageLogs
            .Where(x => x.ReportDefinitionId == reportId);

        var totalRuns = await query.CountAsync();
        var avgTime = await query.AverageAsync(x => x.ExecutionTimeMs);
        var totalRows = await query.SumAsync(x => x.RowCount);

        totalRuns.Should().Be(10);
        avgTime.Should().Be(1450);
        totalRows.Should().Be(1450);
    }

    [Fact]
    public void DashboardWidget_ActivateDeactivate_TogglesState()
    {
        var widget = new DashboardWidget(
            Guid.NewGuid(),
            "dashboard-1",
            "Test Widget",
            "StatCard",
            "Static",
            null,
            null,
            0,
            0,
            3,
            2);

        widget.IsActive.Should().BeTrue();

        widget.Deactivate();
        widget.IsActive.Should().BeFalse();

        widget.Activate();
        widget.IsActive.Should().BeTrue();
    }

    private static List<CategoryNode> BuildCategoryTree(List<ReportCategory> all, string? parentId)
    {
        return all
            .Where(x => x.ParentId == parentId)
            .Select(x => new CategoryNode
            {
                Name = x.Name,
                Children = BuildCategoryTree(all, x.Id.ToString())
            })
            .ToList();
    }

    private class CategoryNode
    {
        public string Name { get; set; } = string.Empty;
        public List<CategoryNode> Children { get; set; } = new();
    }
}
