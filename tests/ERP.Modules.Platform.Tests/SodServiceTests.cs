using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.Modules.Platform.Tests;

public class SodServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock;

    public SodServiceTests()
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
    public async Task CheckConflictAsyncWithMatchingRuleReturnsTrue()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var rule = new SoDRule("AP", "CreateVoucher", "ApproveVoucher", "Cannot create and approve own voucher", "Voucher", 0);
        rule.Activate();
        context.SoDRules.Add(rule);

        var auditLog = new AuditLog("CreateVoucher", "Voucher", Guid.NewGuid(), "user1");
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();

        var service = new SodService(context, _auditLogMock.Object);
        var result = await service.CheckConflictAsync("AP", "Voucher", "user1", "ApproveVoucher", 500);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckConflictAsyncWithNoMatchingRuleReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var service = new SodService(context, _auditLogMock.Object);
        var result = await service.CheckConflictAsync("AP", "Voucher", "user1", "CreateVoucher", 500);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckConflictAsyncWithAmountBelowThresholdReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var rule = new SoDRule("AP", "CreateVoucher", "ApproveVoucher", "Cannot", "Voucher", 1000);
        rule.Activate();
        context.SoDRules.Add(rule);
        await context.SaveChangesAsync();

        var service = new SodService(context, _auditLogMock.Object);
        var result = await service.CheckConflictAsync("AP", "Voucher", "user1", "CreateVoucher", 500);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task LogConflictAsyncCreatesConflictRecord()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var rule = new SoDRule("AP", "Create", "Approve", "Cannot", "Voucher");
        context.SoDRules.Add(rule);
        await context.SaveChangesAsync();

        var service = new SodService(context, _auditLogMock.Object);
        var docId = Guid.NewGuid();
        await service.LogConflictAsync(rule.Id, "user1", "AP", "Voucher", docId, "SameUserBothActions");

        var conflicts = await context.SoDConflicts.ToListAsync();
        conflicts.Should().ContainSingle();
        conflicts[0].RuleId.Should().Be(rule.Id);
        conflicts[0].UserId.Should().Be("user1");
        conflicts[0].Module.Should().Be("AP");
        conflicts[0].DocumentType.Should().Be("Voucher");
        conflicts[0].DocumentId.Should().Be(docId);
        conflicts[0].ConflictType.Should().Be("SameUserBothActions");
        conflicts[0].Resolved.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveRulesAsyncReturnsOnlyActiveRules()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var activeRule = new SoDRule("AP", "Create", "Approve", "Active rule", "Voucher");
        activeRule.Activate();
        var inactiveRule = new SoDRule("AP", "Delete", "View", "Inactive rule", "Voucher");
        inactiveRule.Deactivate();
        context.SoDRules.AddRange(activeRule, inactiveRule);
        await context.SaveChangesAsync();

        var service = new SodService(context, _auditLogMock.Object);
        var rules = await service.GetActiveRulesAsync();

        rules.Should().ContainSingle();
        rules.Should().AllSatisfy(r => r.IsActive.Should().BeTrue());
    }
}
