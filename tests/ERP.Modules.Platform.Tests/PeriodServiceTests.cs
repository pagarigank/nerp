using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.Modules.Platform.Tests;

public class PeriodServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock;

    public PeriodServiceTests()
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

    private static FiscalPeriod CreatePeriod(Guid fiscalYearId, Guid companyId, int periodNumber, string description, int year, int month, int day, int endDay)
    {
        return new FiscalPeriod(
            fiscalYearId,
            companyId,
            periodNumber,
            description,
            new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(year, month, endDay, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetCurrentPeriodAsyncReturnsOpenPeriod()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var companyId = Guid.NewGuid();
        var fiscalYearId = Guid.NewGuid();

        var openPeriod = CreatePeriod(fiscalYearId, companyId, 1, "January 2024", 2024, 1, 1, 31);
        var closedPeriod = CreatePeriod(fiscalYearId, companyId, 2, "February 2024", 2024, 2, 1, 29);
        closedPeriod.Close();

        context.FiscalPeriods.AddRange(openPeriod, closedPeriod);
        await context.SaveChangesAsync();

        var service = new PeriodService(context, _auditLogMock.Object);
        var result = await service.GetCurrentPeriodAsync(companyId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(openPeriod.Id);
        result.Status.Should().Be(PeriodStatus.Open);
    }

    [Fact]
    public async Task IsPeriodOpenAsyncChecksDateRangeCorrectly()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var companyId = Guid.NewGuid();
        var fiscalYearId = Guid.NewGuid();

        var period = CreatePeriod(fiscalYearId, companyId, 1, "January 2024", 2024, 1, 1, 31);

        context.FiscalPeriods.Add(period);
        await context.SaveChangesAsync();

        var service = new PeriodService(context, _auditLogMock.Object);

        var insideDate = await service.IsPeriodOpenAsync(companyId, new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero));
        var outsideDate = await service.IsPeriodOpenAsync(companyId, new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero));

        insideDate.Should().BeTrue();
        outsideDate.Should().BeFalse();
    }

    [Fact]
    public async Task ClosePeriodAsyncSetsStatusToClosed()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var companyId = Guid.NewGuid();
        var period = new FiscalPeriod(
            Guid.NewGuid(),
            companyId,
            1,
            "Test Period",
            DateTimeOffset.UtcNow.AddMonths(-1),
            DateTimeOffset.UtcNow.AddMonths(1));

        context.FiscalPeriods.Add(period);
        await context.SaveChangesAsync();

        var service = new PeriodService(context, _auditLogMock.Object);
        await service.ClosePeriodAsync(period.Id, "admin", new[] { "Admin" });

        var saved = await context.FiscalPeriods.FindAsync(period.Id);
        saved!.Status.Should().Be(PeriodStatus.Closed);
    }

    [Fact]
    public async Task ClosePeriodAsyncThrowsForNonOpenPeriod()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var period = new FiscalPeriod(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Test Period",
            DateTimeOffset.UtcNow.AddMonths(-1),
            DateTimeOffset.UtcNow.AddMonths(1));
        period.Close();

        context.FiscalPeriods.Add(period);
        await context.SaveChangesAsync();

        var service = new PeriodService(context, _auditLogMock.Object);
        var act = async () => await service.ClosePeriodAsync(period.Id, "admin", new[] { "Admin" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not open*");
    }

    [Fact]
    public async Task OpenPeriodAsyncThrowsForNonAdminRole()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var period = new FiscalPeriod(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Test Period",
            DateTimeOffset.UtcNow.AddMonths(-1),
            DateTimeOffset.UtcNow.AddMonths(1));
        period.Close();

        context.FiscalPeriods.Add(period);
        await context.SaveChangesAsync();

        var service = new PeriodService(context, _auditLogMock.Object);
        var act = async () => await service.OpenPeriodAsync(period.Id, "user", new[] { "User" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Only an administrator*");
    }
}
