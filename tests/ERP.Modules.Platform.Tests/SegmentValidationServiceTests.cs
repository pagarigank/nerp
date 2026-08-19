using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.Modules.Platform.Tests;

public class SegmentValidationServiceTests
{
    [Fact]
    public async Task ValidateCombinationAsyncWithValidCombinationReturnsTrue()
    {
        var companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var segmentType = new SegmentType(companyId, "Account", "ACCT", 1, true);
        context.SegmentTypes.Add(segmentType);

        var segmentValue = new SegmentValue(segmentType.Id, companyId, "1000", "Cash", 1);
        context.SegmentValues.Add(segmentValue);

        var dict = new Dictionary<string, string> { { "ACCT", "1000" } };
        var key = SegmentValidationService.BuildCombinationKey(dict);
        var combo = new ValidatedCombination(companyId, key, "{}");
        context.ValidatedCombinations.Add(combo);

        await context.SaveChangesAsync();

        var service = new SegmentValidationService(context);
        var result = await service.ValidateCombinationAsync(companyId, dict);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCombinationAsyncWithMissingRequiredSegmentReturnsFalse()
    {
        var companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var segmentType = new SegmentType(companyId, "Account", "ACCT", 1, true);
        context.SegmentTypes.Add(segmentType);
        await context.SaveChangesAsync();

        var service = new SegmentValidationService(context);
        var result = await service.ValidateCombinationAsync(companyId, new Dictionary<string, string>());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCombinationAsyncWithInactiveSegmentReturnsFalse()
    {
        var companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var segmentType = new SegmentType(companyId, "Account", "ACCT", 1, true);
        context.SegmentTypes.Add(segmentType);

        var segmentValue = new SegmentValue(segmentType.Id, companyId, "1000", "Cash", 1);
        segmentValue.Deactivate();
        context.SegmentValues.Add(segmentValue);
        await context.SaveChangesAsync();

        var service = new SegmentValidationService(context);
        var result = await service.ValidateCombinationAsync(companyId, new Dictionary<string, string> { { "ACCT", "1000" } });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCombinationAsyncWithEmptyDictReturnsTrue()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);
        var service = new SegmentValidationService(context);

        var result1 = await service.ValidateCombinationAsync(Guid.NewGuid(), new Dictionary<string, string>());
        var result2 = await service.ValidateCombinationAsync(Guid.NewGuid(), null!);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    [Fact]
    public void BuildCombinationKeyBuildsCorrectKey()
    {
        var dict = new Dictionary<string, string>
        {
            { "DEPT", "0100" },
            { "ACCT", "1000" }
        };

        var key = SegmentValidationService.BuildCombinationKey(dict);

        key.Should().Be("ACCT=1000:DEPT=0100");
    }
}
