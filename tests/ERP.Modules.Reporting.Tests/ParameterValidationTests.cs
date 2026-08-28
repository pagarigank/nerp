// <copyright file="ParameterValidationTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.Reporting.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.Reporting.Tests;

public class ParameterValidationTests
{
    [Fact]
    public void ReportParameterSet_Create_SetsAllProperties()
    {
        var companyId = Guid.NewGuid();
        var reportDefId = Guid.NewGuid();
        var parameters = JsonSerializer.Serialize(new
        {
            CompanyId = companyId,
            PeriodId = Guid.NewGuid(),
            DateFrom = DateTimeOffset.UtcNow.AddDays(-30),
            DateTo = DateTimeOffset.UtcNow
        });

        var set = new ReportParameterSet(
            companyId,
            reportDefId,
            "Last 30 Days",
            parameters,
            true,
            "Default parameters for aging reports");

        set.CompanyId.Should().Be(companyId);
        set.ReportDefinitionId.Should().Be(reportDefId);
        set.Name.Should().Be("Last 30 Days");
        set.ParametersJson.Should().Be(parameters);
        set.IsDefault.Should().BeTrue();
        set.Description.Should().Be("Default parameters for aging reports");
        set.RunCount.Should().Be(0);
    }

    [Fact]
    public void ReportParameterSet_IncrementRunCount_Increments()
    {
        var set = new ReportParameterSet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test",
            "{}",
            false,
            null);

        set.IncrementRunCount();
        set.IncrementRunCount();
        set.IncrementRunCount();

        set.RunCount.Should().Be(3);
    }

    [Fact]
    public void ReportParameterSet_Update_UpdatesProperties()
    {
        var set = new ReportParameterSet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Original",
            "{}",
            false,
            "Original description");

        set.Update("Updated", "{\"key\":\"value\"}", true, "Updated description");

        set.Name.Should().Be("Updated");
        set.ParametersJson.Should().Be("{\"key\":\"value\"}");
        set.IsDefault.Should().BeTrue();
        set.Description.Should().Be("Updated description");
    }

    [Fact]
    public void ReportParameterSet_SetDefault_TogglesState()
    {
        var set = new ReportParameterSet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test",
            "{}",
            false,
            null);

        set.IsDefault.Should().BeFalse();

        set.SetDefault(true);
        set.IsDefault.Should().BeTrue();

        set.SetDefault(false);
        set.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void ReportCategory_Create_SetsAllProperties()
    {
        var category = new ReportCategory(
            Guid.NewGuid(),
            "Financial Reports",
            null,
            1,
            "Standard financial statement reports",
            "calculator");

        category.Name.Should().Be("Financial Reports");
        category.ParentId.Should().BeNull();
        category.SortOrder.Should().Be(1);
        category.Description.Should().Be("Standard financial statement reports");
        category.Icon.Should().Be("calculator");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReportCategory_Create_WithParent_SetsParentId()
    {
        var parentId = Guid.NewGuid();

        var category = new ReportCategory(
            Guid.NewGuid(),
            "Balance Sheets",
            parentId.ToString(),
            2,
            null,
            null);

        category.ParentId.Should().Be(parentId.ToString());
    }

    [Fact]
    public void ReportCategory_Update_UpdatesProperties()
    {
        var category = new ReportCategory(
            Guid.NewGuid(),
            "Old Name",
            null,
            1,
            "Old description",
            "old-icon");

        category.Update("New Name", null, 5, "New description", "new-icon");

        category.Name.Should().Be("New Name");
        category.SortOrder.Should().Be(5);
        category.Description.Should().Be("New description");
        category.Icon.Should().Be("new-icon");
    }

    [Fact]
    public void ReportCategory_ActivateDeactivate_TogglesState()
    {
        var category = new ReportCategory(
            Guid.NewGuid(),
            "Test",
            null,
            1,
            null,
            null);

        category.IsActive.Should().BeTrue();

        category.Deactivate();
        category.IsActive.Should().BeFalse();

        category.Activate();
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReportDefinition_Create_SetsAllProperties()
    {
        var companyId = Guid.NewGuid();
        var report = new ReportDefinition(
            companyId,
            "AP Aging",
            "AccountsPayable",
            "Aging",
            "Accounts Payable aging by vendor",
            "Standard",
            "dbo.ApAging",
            "SELECT * FROM ApAging WHERE CompanyId = @CompanyId",
            "{\"CompanyId\":{\"type\":\"guid\",\"required\":true}}",
            "{\"columns\":[\"Vendor\",\"Current\",\"30\",\"60\",\"90+\"]}");

        report.CompanyId.Should().Be(companyId);
        report.Name.Should().Be("AP Aging");
        report.Module.Should().Be("AccountsPayable");
        report.Category.Should().Be("Aging");
        report.Description.Should().Be("Accounts Payable aging by vendor");
        report.ReportType.Should().Be("Standard");
        report.DataSource.Should().Be("dbo.ApAging");
        report.IsActive.Should().BeTrue();
        report.IsShared.Should().BeFalse();
    }

    [Fact]
    public void ReportDefinition_Share_TogglesShared()
    {
        var report = new ReportDefinition(
            Guid.NewGuid(),
            "Test Report",
            "GL",
            "Test",
            "Description",
            "Standard");

        report.IsShared.Should().BeFalse();

        report.SetShared(true);
        report.IsShared.Should().BeTrue();

        report.SetShared(false);
        report.IsShared.Should().BeFalse();
    }

    [Fact]
    public void ReportDefinition_ActivateDeactivate_TogglesState()
    {
        var report = new ReportDefinition(
            Guid.NewGuid(),
            "Test",
            "GL",
            "Test",
            "Description",
            "Standard");

        report.IsActive.Should().BeTrue();

        report.Deactivate();
        report.IsActive.Should().BeFalse();

        report.Activate();
        report.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReportUsageLog_Create_TracksExecution()
    {
        var log = new ReportUsageLog(
            Guid.NewGuid(),
            "Standard",
            Guid.NewGuid(),
            null,
            "testuser@erp.com",
            "{\"CompanyId\":\"00000000-0000-0000-0000-000000000001\"}",
            "CSV",
            1500,
            250);

        log.ReportType.Should().Be("Standard");
        log.ExecutedByUser.Should().Be("testuser@erp.com");
        log.ExportFormat.Should().Be("CSV");
        log.ExecutionTimeMs.Should().Be(1500);
        log.RowCount.Should().Be(250);
        log.Status.Should().Be("Success");
        log.ExecutedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ReportUsageLog_MarkFailed_SetsErrorState()
    {
        var log = new ReportUsageLog(
            Guid.NewGuid(),
            "Standard",
            null,
            null,
            "user",
            null,
            "Screen",
            0,
            0);

        log.MarkFailed("Database connection timeout");

        log.Status.Should().Be("Failed");
        log.ErrorMessage.Should().Be("Database connection timeout");
    }
}
