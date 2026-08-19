// <copyright file="RequisitionToPOApprovalThresholdTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests.Purchasing;

/// <summary>
/// Proves the requisition -> PO approval threshold (todo.md line 321): a
/// requisition whose total exceeds $10,000 cannot be converted to a PO unless it
/// was approved by a manager (a user other than the requestor). Self-approval of a
/// high-value requisition is rejected.
/// </summary>
public class RequisitionToPOApprovalThresholdTests : IntegrationTestBase
{
    [Fact]
    public async Task HighValueRequisition_SelfApproved_CannotConvertToPO()
    {
        await CleanDatabaseAsync();

        var (requisitionId, service) = await SeedHighValueRequisitionAsync(selfApproved: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.ConvertRequisitionToPOAsync(requisitionId, null));

        ex.Message.Should().Contain("manager approval");
    }

    [Fact]
    public async Task HighValueRequisition_ManagerApproved_CanConvertToPO()
    {
        await CleanDatabaseAsync();

        var (requisitionId, service) = await SeedHighValueRequisitionAsync(selfApproved: false);

        var poId = await service.ConvertRequisitionToPOAsync(requisitionId, null);

        poId.Should().NotBe(Guid.Empty, "manager-approved high-value requisition can be converted to a PO");
    }

    [Fact]
    public async Task LowValueRequisition_SelfApproved_CanConvertToPO()
    {
        await CleanDatabaseAsync();

        var (requisitionId, service) = await SeedLowValueRequisitionAsync();

        var poId = await service.ConvertRequisitionToPOAsync(requisitionId, null);

        poId.Should().NotBe(Guid.Empty, "below-threshold requisitions do not require manager approval");
    }

    private async Task<(Guid, IRequisitionToPOService)> SeedHighValueRequisitionAsync(bool selfApproved)
    {
        Guid requisitionId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"RQTH-{Guid.NewGuid():N}", "Req Threshold Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var purchasing = sp.GetRequiredService<PurchasingDbContext>();
            var requestorId = Guid.NewGuid();
            var approverId = selfApproved ? requestorId : Guid.NewGuid(); // manager != requestor

            var requisition = new Requisition(
                $"REQ-{Guid.NewGuid():N}", company.Id, requestorId, DateTime.UtcNow, null, "High value req");

            // 2 lines * 6000 = 12000 > 10000 threshold.
            requisition.AddLine(new RequisitionLine(
                requisition.Id, 1, null, "Server", 2m, "EA", 6000m, null, company.Id, null, null, null));
            requisition.AddLine(new RequisitionLine(
                requisition.Id, 2, null, "Switch", 1m, "EA", 6000m, null, company.Id, null, null, null));

            purchasing.Requisitions.Add(requisition);
            await purchasing.SaveChangesAsync();

            requisition.Approve(approverId);
            await purchasing.SaveChangesAsync();

            requisitionId = requisition.Id;
        });

        var service = ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IRequisitionToPOService>();
        return (requisitionId, service);
    }

    private async Task<(Guid, IRequisitionToPOService)> SeedLowValueRequisitionAsync()
    {
        Guid requisitionId = Guid.Empty;
        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            var company = new Company($"RQTL-{Guid.NewGuid():N}", "Req Low Co", "USD", null, null, null);
            await platform.Companies.AddAsync(company);

            var purchasing = sp.GetRequiredService<PurchasingDbContext>();
            var requestorId = Guid.NewGuid();

            var requisition = new Requisition(
                $"REQ-{Guid.NewGuid():N}", company.Id, requestorId, DateTime.UtcNow, null, "Low value req");

            // 1 line * 500 = 500 < 10000 threshold.
            requisition.AddLine(new RequisitionLine(
                requisition.Id, 1, null, "Toner", 10m, "EA", 50m, null, company.Id, null, null, null));

            purchasing.Requisitions.Add(requisition);
            await purchasing.SaveChangesAsync();

            requisition.Approve(requestorId); // self-approval is fine below threshold
            await purchasing.SaveChangesAsync();

            requisitionId = requisition.Id;
        });

        var service = ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IRequisitionToPOService>();
        return (requisitionId, service);
    }
}
