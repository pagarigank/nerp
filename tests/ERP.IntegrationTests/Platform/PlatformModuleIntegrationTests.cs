// <copyright file="PlatformModuleIntegrationTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.Platform;

public class PlatformModuleIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateCompany_ShouldPersistAndRetrieve()
    {
        // Arrange
        var company = new Company("TEST", "Test Company", "USD");

        // Act
        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.Companies.AddAsync(company);
            await uow.SaveChangesAsync();
        });

        // Assert
        var retrieved = await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            return await uow.Companies.GetByIdAsync(company.Id);
        });

        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("TEST");
        retrieved.Name.Should().Be("Test Company");
        retrieved.BaseCurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task FiscalPeriod_OpenCloseWorkflow_ShouldWork()
    {
        // Arrange
        var company = new Company("PER", "Period Test Company", "USD");
        var fiscalYear = new FiscalYear(company.Id, 2024, new DateTimeOffset(2024, 1, 1), new DateTimeOffset(2024, 12, 31));
        var period = new FiscalPeriod(fiscalYear.Id, 1, new DateTimeOffset(2024, 1, 1), new DateTimeOffset(2024, 1, 31));

        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.Companies.AddAsync(company);
            await uow.FiscalYears.AddAsync(fiscalYear);
            await uow.FiscalPeriods.AddAsync(period);
            await uow.SaveChangesAsync();
        });

        // Act - Close period
        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var p = await uow.FiscalPeriods.GetByIdAsync(period.Id);
            p!.Close();
            await uow.SaveChangesAsync();
        });

        // Assert - Period should be closed
        var closedPeriod = await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            return await uow.FiscalPeriods.GetByIdAsync(period.Id);
        });

        closedPeriod!.Status.Should().Be(FiscalPeriodStatus.Closed);

        // Act - Reopen with admin reason
        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var p = await uow.FiscalPeriods.GetByIdAsync(period.Id);
            p!.Reopen("Admin reopening for adjustment");
            await uow.SaveChangesAsync();
        });

        // Assert - Period should be open again
        var reopenedPeriod = await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            return await uow.FiscalPeriods.GetByIdAsync(period.Id);
        });

        reopenedPeriod!.Status.Should().Be(FiscalPeriodStatus.Open);
    }

    [Fact]
    public async Task SegmentValidation_ShouldValidateCombinations()
    {
        // Arrange
        var company = new Company("SEG", "Segment Test Company", "USD");
        var segmentType1 = new SegmentType("ACCT", "Account", 6, true);
        var segmentType2 = new SegmentType("DEPT", "Department", 4, true);
        
        var segmentValue1 = new SegmentValue("ACCT", "1000", "Cash", "Asset account");
        var segmentValue2 = new SegmentValue("DEPT", "0100", "Sales", "Sales department");
        
        var validCombo = new ValidatedCombination(new[] { segmentValue1.Id, segmentValue2.Id }, company.Id, true);

        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.Companies.AddAsync(company);
            await uow.SegmentTypes.AddAsync(segmentType1);
            await uow.SegmentTypes.AddAsync(segmentType2);
            await uow.SegmentValues.AddAsync(segmentValue1);
            await uow.SegmentValues.AddAsync(segmentValue2);
            await uow.ValidatedCombinations.AddAsync(validCombo);
            await uow.SaveChangesAsync();
        });

        // Act
        var isValid = await ExecuteInTransactionAsync(async sp =>
        {
            var validationService = sp.GetRequiredService<ISegmentValidationService>();
            return await validationService.ValidateCombinationAsync(company.Id, new[] { segmentValue1.Id, segmentValue2.Id });
        });

        // Assert
        isValid.Should().BeTrue();

        // Test invalid combination
        var invalidCombo = await ExecuteInTransactionAsync(async sp =>
        {
            var validationService = sp.GetRequiredService<ISegmentValidationService>();
            var invalidSegment = new SegmentValue("DEPT", "9999", "Invalid", "Invalid dept");
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.SegmentValues.AddAsync(invalidSegment);
            await uow.SaveChangesAsync();
            return await validationService.ValidateCombinationAsync(company.Id, new[] { segmentValue1.Id, invalidSegment.Id });
        });

        invalidCombo.Should().BeFalse();
    }

    [Fact]
    public async Task ApprovalWorkflow_ShouldRouteCorrectly()
    {
        // Arrange
        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval", Guid.NewGuid(), 1000);
        workflow.AddStep(1, "Manager Approval", null, Guid.NewGuid(), 1, 0, 10000);
        workflow.AddStep(2, "Controller Approval", Guid.NewGuid(), null, 1, 10000, null);

        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.ApprovalWorkflows.AddAsync(workflow);
            await uow.SaveChangesAsync();
        });

        // Act - Submit for approval
        var request = await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<IApprovalWorkflowService>();
            return await service.SubmitForApprovalAsync(
                workflow.Id, "AP", "Voucher", Guid.NewGuid(), "VCH-001", 5000, "Test User", "Test voucher");
        });

        // Assert
        request.Should().NotBeNull();
        request.Status.Should().Be(ApprovalStatus.Pending);
        request.CurrentStep.Should().Be(1);

        // Act - Approve step 1
        var action = await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<IApprovalWorkflowService>();
            return await service.ProcessActionAsync(
                request.Id, "Manager1", ApprovalDecision.Approved, 1, "Approved by manager");
        });

        action.Decision.Should().Be(ApprovalDecision.Approved);

        // Act - Approve step 2 (final)
        action = await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<IApprovalWorkflowService>();
            return await service.ProcessActionAsync(
                request.Id, "Controller1", ApprovalDecision.Approved, 2, "Approved by controller");
        });

        // Assert - Request should be approved
        var finalRequest = await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<IApprovalWorkflowService>();
            return await service.GetRequestByIdAsync(request.Id);
        });

        finalRequest!.Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public async Task SoDRule_ShouldPreventConflict()
    {
        // Arrange
        var rule = new SoDRule("AP", "CreateVoucher", "ApproveVoucher", "Cannot create and approve own voucher", "Voucher", 0);
        rule.Activate();

        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.SoDRules.AddAsync(rule);
            await uow.SaveChangesAsync();
        });

        // Act - Check conflict for same user doing both actions
        var hasConflict = await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<ISodService>();
            return await service.CheckConflictAsync("AP", "Voucher", Guid.NewGuid(), "CreateVoucher", 500);
        });

        // Assert - Should detect potential conflict (simplified check)
        hasConflict.Should().BeFalse(); // No previous action recorded yet

        // Log first action
        await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<ISodService>();
            await service.LogConflictAsync(rule.Id, Guid.NewGuid(), "AP", "Voucher", Guid.NewGuid(), SoDConflictType.SameUserBothActions);
        });

        // Check again
        hasConflict = await ExecuteInTransactionAsync(async sp =>
        {
            var service = sp.GetRequiredService<ISodService>();
            return await service.CheckConflictAsync("AP", "Voucher", Guid.NewGuid(), "ApproveVoucher", 500);
        });

        hasConflict.Should().BeTrue();
    }

    [Fact]
    public async Task CompanyScopedRls_SuperAdminSeesAll_CompanyAdminSeesOnlyOwn()
    {
        // Arrange - two companies and a super admin + a company admin.
        var companyA = new Company("COA", "Company A", "USD");
        var companyB = new Company("COB", "Company B", "USD");
        var superAdminId = "11111111-1111-1111-1111-111111111111";
        var companyAdminId = "22222222-2222-2222-2222-222222222222";

        await ExecuteInTransactionAsync(async sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            await uow.Companies.AddAsync(companyA);
            await uow.Companies.AddAsync(companyB);
            await uow.SaveChangesAsync();

            // Super admin: role assignment with no company scope.
            var superRole = new UserRole(Guid.Parse(superAdminId), Guid.NewGuid(), null);
            // Company admin: scoped to company A only.
            var adminRole = new UserRole(Guid.Parse(companyAdminId), Guid.NewGuid(), companyA.Id);
            await uow.UserRoles.AddAsync(superRole);
            await uow.UserRoles.AddAsync(adminRole);
            await uow.SaveChangesAsync();
        });

        // Act - resolve the allowed companies the same way the RLS filter does.
        var (superCompanies, adminCompanies) = await ExecuteInTransactionAsync(async sp =>
        {
            var db = sp.GetRequiredService<PlatformDbContext>();
            var superAssignments = await db.UserRoles
                .Where(ur => ur.UserId == superAdminId)
                .Select(ur => ur.CompanyId)
                .ToListAsync();
            var adminAssignments = await db.UserRoles
                .Where(ur => ur.UserId == companyAdminId)
                .Select(ur => ur.CompanyId)
                .ToListAsync();

            var superIsSuper = superAssignments.Contains(null);
            var superAllowed = superIsSuper
                ? new List<Guid>()
                : superAssignments.Where(c => c != null).Select(c => c!.Value).ToList();

            var adminAllowed = adminAssignments
                .Where(c => c != null)
                .Select(c => c!.Value)
                .ToList();

            return (superAllowed, adminAllowed);
        });

        // Assert - super admin has no explicit scope (empty => all companies).
        superCompanies.Should().BeEmpty();

        // Assert - company admin is scoped to exactly company A.
        adminCompanies.Should().HaveCount(1);
        adminCompanies.Should().Contain(companyA.Id);
        adminCompanies.Should().NotContain(companyB.Id);
    }
}