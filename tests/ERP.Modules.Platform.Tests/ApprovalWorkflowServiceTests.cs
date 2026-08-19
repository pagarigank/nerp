using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.Modules.Platform.Tests;

public class ApprovalWorkflowServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock;

    public ApprovalWorkflowServiceTests()
    {
        _auditLogMock = new Mock<IAuditLogService>();
        _auditLogMock
            .Setup(x => x.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task GetWorkflowAsyncWithMatchingModuleDocumentTypeAndAmountReturnsWorkflow()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval", null, 500);
        workflow.AddStep(1, "Manager Approval", null, Guid.NewGuid(), 1);
        context.ApprovalWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var service = new ApprovalWorkflowService(context, _auditLogMock.Object);
        var result = await service.GetWorkflowAsync("AP", "Voucher", 1000);

        result.Should().NotBeNull();
        result!.Id.Should().Be(workflow.Id);
    }

    [Fact]
    public async Task SubmitForApprovalAsyncCreatesRequestInPendingStatus()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval");
        context.ApprovalWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var service = new ApprovalWorkflowService(context, _auditLogMock.Object);
        var request = await service.SubmitForApprovalAsync(
            workflow.Id, "AP", "Voucher", Guid.NewGuid(), "VCH-001", 5000, "TestUser", "Test voucher");

        request.Should().NotBeNull();
        request.Status.Should().Be(ApprovalStatus.Pending);
        request.Module.Should().Be("AP");
        request.DocumentType.Should().Be("Voucher");
        request.DocumentNumber.Should().Be("VCH-001");
        request.Amount.Should().Be(5000);
        request.RequestedBy.Should().Be("TestUser");
        request.CurrentStep.Should().Be(1);
    }

    [Fact]
    public async Task ProcessActionAsyncApprovesAndAdvancesSteps()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval", null, 1000);
        workflow.AddStep(1, "Manager Approval", null, Guid.NewGuid(), 1);
        workflow.AddStep(2, "Controller Approval", Guid.NewGuid(), null, 1);
        context.ApprovalWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var service = new ApprovalWorkflowService(context, _auditLogMock.Object);
        var request = await service.SubmitForApprovalAsync(
            workflow.Id, "AP", "Voucher", Guid.NewGuid(), "VCH-001", 5000, "TestUser", null);

        var savedWorkflow = await context.ApprovalWorkflows
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .FirstAsync(w => w.Id == workflow.Id);
        var step1 = savedWorkflow.Steps[0];
        var step2 = savedWorkflow.Steps[1];

        await service.ProcessActionAsync(request.Id, "Manager1", ApprovalDecision.Approved, step1.Id, "OK by mgr");

        request.Status.Should().Be(ApprovalStatus.PartiallyApproved);
        request.CurrentStep.Should().Be(2);

        await service.ProcessActionAsync(request.Id, "Controller1", ApprovalDecision.Approved, step2.Id, "OK by ctrl");

        request.Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public async Task ProcessActionAsyncRejectsRequest()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval");
        context.ApprovalWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var service = new ApprovalWorkflowService(context, _auditLogMock.Object);
        var request = await service.SubmitForApprovalAsync(
            workflow.Id, "AP", "Voucher", Guid.NewGuid(), "VCH-001", 5000, "TestUser", null);

        var action = await service.ProcessActionAsync(request.Id, "Manager1", ApprovalDecision.Rejected, null, "Not approved");

        action.Decision.Should().Be(ApprovalDecision.Rejected);
        request.Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public async Task ProcessActionAsyncWithCompletedRequestThrows()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval");
        context.ApprovalWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var service = new ApprovalWorkflowService(context, _auditLogMock.Object);
        var request = await service.SubmitForApprovalAsync(
            workflow.Id, "AP", "Voucher", Guid.NewGuid(), "VCH-001", 5000, "TestUser", null);

        await service.ProcessActionAsync(request.Id, "Manager1", ApprovalDecision.Rejected, null, "No");

        var act = async () => await service.ProcessActionAsync(request.Id, "Manager2", ApprovalDecision.Approved, null, "Too late");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already Rejected*");
    }

    [Fact]
    public async Task CanUserApproveAsyncReturnsFalseForSameUser()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var workflow = new ApprovalWorkflow("AP", "Voucher", "AP Voucher Approval");
        context.ApprovalWorkflows.Add(workflow);
        await context.SaveChangesAsync();

        var service = new ApprovalWorkflowService(context, _auditLogMock.Object);
        var request = await service.SubmitForApprovalAsync(
            workflow.Id, "AP", "Voucher", Guid.NewGuid(), "VCH-001", 5000, "SameUser", null);

        var canApprove = await service.CanUserApproveAsync(request.Id, "SameUser");

        canApprove.Should().BeFalse();
    }
}
