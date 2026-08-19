// <copyright file="SodEnforcementTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.Platform;

/// <summary>
/// Proves the Separation-of-Duties engine is now actually enforced (previously
/// the SodService read an always-empty AuditLogs table and the audit interceptor
/// only ran on PlatformDbContext, so conflicts were never detected). This test
/// seeds a "create vs post" SoD rule, then posts a voucher batch as the same user
/// who created it (must be rejected) and as a different user (must be allowed).
/// </summary>
public class SodEnforcementTests : IntegrationTestBase
{
    private static readonly DateTimeOffset PostingDate = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SameUserCreateAndPost_ShouldBeRejected()
    {
        await CleanDatabaseAsync();
        var creator = "user-alice";

        var company = new Company($"SOD-{Guid.NewGuid():N}", "SoD Co", "USD", null, null, null);
        await SeedAsync(company, creator);
        var batchId = await CreateVoucherBatchAsync(company.Id, creator);

        // Post as the same user who created -> SoD conflict expected.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PostVoucherBatchAsync(batchId, creator));

        ex.Message.Should().Contain("Separation of Duties");
    }

    [Fact]
    public async Task DifferentUserPost_ShouldBeAllowed()
    {
        await CleanDatabaseAsync();
        var creator = "user-alice";
        var poster = "user-bob";

        var company = new Company($"SOD-{Guid.NewGuid():N}", "SoD Co", "USD", null, null, null);
        await SeedAsync(company, creator);
        var batchId = await CreateVoucherBatchAsync(company.Id, creator);

        // Post as a different user -> no conflict.
        var posted = await PostVoucherBatchAsync(batchId, poster);
        posted.Should().NotBeNull();
    }

    private async Task SeedAsync(Company company, string seedUser)
    {
        using var scope = ServiceProvider.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        // Impersonate the seed user so the audit interceptor records the rule owner.
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>() as CurrentUserService;
        if (currentUser is not null)
            currentUser.UserId = seedUser;

        await platform.Companies.AddAsync(company);

        // Open fiscal year/period so the posting gate is passed and SoD is reached.
        var fiscalYear = new FiscalYear(company.Id, 2026, "FY2026",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var period = new FiscalPeriod(fiscalYear.Id, company.Id, 1, "2026-01",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));
        await platform.FiscalYears.AddAsync(fiscalYear);
        await platform.FiscalPeriods.AddAsync(period);

        // Remove any pre-existing create/post rule (the unique index on
        // Module+ActionA+ActionB would otherwise reject a re-seed between tests).
        var existing = await platform.SoDRules
            .Where(r => r.Module == "AccountsPayable" && r.ActionA == "Created" && r.ActionB == "Post")
            .ToListAsync();
        platform.SoDRules.RemoveRange(existing);
        await platform.SaveChangesAsync();

        await platform.SoDRules.AddAsync(new SoDRule(
            "AccountsPayable", "Created", "Post",
            "Creator may not post the same voucher batch", nameof(VoucherBatch), null));
        await platform.SaveChangesAsync();
    }

    private async Task<Guid> CreateVoucherBatchAsync(Guid companyId, string user)
    {
        using var scope = ServiceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>() as CurrentUserService;
        if (currentUser is not null)
            currentUser.UserId = user;

        var ap = scope.ServiceProvider.GetRequiredService<ApDbContext>();

        var vendor = new Vendor($"V-{Guid.NewGuid():N}", "Test Vendor", null, null, null, null, true);
        ap.Vendors.Add(vendor);
        await ap.SaveChangesAsync();

        var batch = new VoucherBatch(companyId, $"VB-{Guid.NewGuid():N}", "Test", PostingDate, Guid.NewGuid());
        ap.VoucherBatches.Add(batch);
        await ap.SaveChangesAsync();

        // A voucher with no distributions is still balanced (0 == 0), so the batch
        // can be released and posted, reaching the SoD check.
        batch.AddVoucher(
            vendor.Id,
            VoucherType.Invoice,
            $"INV-{Guid.NewGuid():N}",
            PostingDate,
            PostingDate.AddDays(30),
            100m,
            0m,
            "Test voucher",
            null);
        await ap.SaveChangesAsync();

        return batch.Id;
    }

    private async Task<VoucherBatch> PostVoucherBatchAsync(Guid batchId, string user)
    {
        using var scope = ServiceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>() as CurrentUserService;
        if (currentUser is not null)
            currentUser.UserId = user;

        var svc = scope.ServiceProvider.GetRequiredService<IVoucherService>();

        // A batch must be released (Draft -> Batched) before it can be posted.
        await svc.ReleaseBatchAsync(batchId);
        return await svc.PostBatchAsync(batchId);
    }
}
