// <copyright file="GrToApMatchTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
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
/// Proves the Purchasing -&gt; AP goods-receipt integration (todo.md line 323):
/// posting a goods receipt raises GoodsReceivedEvent, which is now consumed by
/// GoodsReceivedToApHandler and persisted as a GoodsReceiptMatch row so the AP
/// 3-way match (PO &lt;-&gt; Receipt &lt;-&gt; Invoice) can correlate received quantities.
/// </summary>
public class GrToApMatchTests : IntegrationTestBase
{
    [Fact]
    public async Task PostReceipt_ShouldRecordGoodsReceiptMatchInAp()
    {
        await CleanDatabaseAsync();

        var company = new Company($"GRAP-{Guid.NewGuid():N}", "GR->AP Co", "USD", null, null, null);
        Guid poLineId = Guid.Empty;

        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);

            var purchasing = sp.GetRequiredService<PurchasingDbContext>();

            var po = new PurchaseOrder(
                $"PO-{Guid.NewGuid():N}", company.Id, Guid.NewGuid(), DateTime.UtcNow,
                PurchaseOrderType.Standard, null, null, null, null, null, null);
            purchasing.PurchaseOrders.Add(po);
            await purchasing.SaveChangesAsync();

            var poLine = new PurchaseOrderLine(
                po.Id, 1, null, "Widgets", 10m, "EA", 12.50m, DateTime.UtcNow, null, null, null);
            purchasing.PurchaseOrderLines.Add(poLine);
            await purchasing.SaveChangesAsync();

            poLineId = poLine.Id;
        });

        // Post a goods receipt linked to the PO line (within tolerance -> no over-receipt).
        using (var postScope = ServiceProvider.CreateScope())
        {
            var purchasing = postScope.ServiceProvider.GetRequiredService<PurchasingDbContext>();

            var receipt = new Receipt(
                $"GR-{Guid.NewGuid():N}", company.Id, null, null, DateTime.UtcNow, "tester", null, null);
            receipt.AddLine(new ReceiptLine(
                receipt.Id, 1, poLineId, null, "Widgets", 10m, "EA", null, null, false, null, null));

            purchasing.Receipts.Add(receipt);
            await purchasing.SaveChangesAsync();

            receipt.Post();
            await purchasing.SaveChangesAsync();
        }

        var match = await ExecuteInTransactionAsync(async sp =>
        {
            var ap = sp.GetRequiredService<ApDbContext>();
            return await ap.GoodsReceiptMatches
                .Where(m => m.PurchaseOrderLineId == poLineId)
                .FirstOrDefaultAsync();
        });

        match.Should().NotBeNull("posting a goods receipt must record the received leg in AP");
        match!.QuantityReceived.Should().Be(10m);
        match.OverReceiptFlag.Should().BeFalse("received qty equals ordered qty (within tolerance)");
    }

    [Fact]
    public async Task PostReceipt_OverReceivedQuantity_ShouldFlagOverReceipt()
    {
        await CleanDatabaseAsync();

        var company = new Company($"GRAPO-{Guid.NewGuid():N}", "GR->AP Over Co", "USD", null, null, null);
        Guid poLineId = Guid.Empty;

        await ExecuteInTransactionAsync(async sp =>
        {
            var platform = sp.GetRequiredService<PlatformDbContext>();
            await platform.Companies.AddAsync(company);

            var purchasing = sp.GetRequiredService<PurchasingDbContext>();

            var po = new PurchaseOrder(
                $"PO-{Guid.NewGuid():N}", company.Id, Guid.NewGuid(), DateTime.UtcNow,
                PurchaseOrderType.Standard, null, null, null, null, null, null);
            purchasing.PurchaseOrders.Add(po);
            await purchasing.SaveChangesAsync();

            var poLine = new PurchaseOrderLine(
                po.Id, 1, null, "Widgets", 10m, "EA", 12.50m, DateTime.UtcNow, null, null, null);
            purchasing.PurchaseOrderLines.Add(poLine);
            await purchasing.SaveChangesAsync();

            poLineId = poLine.Id;
        });

        // Receive 12 (20% over the ordered 10) -> exceeds the 5% over-receipt tolerance.
        using (var postScope = ServiceProvider.CreateScope())
        {
            var purchasing = postScope.ServiceProvider.GetRequiredService<PurchasingDbContext>();

            var receipt = new Receipt(
                $"GR-{Guid.NewGuid():N}", company.Id, null, null, DateTime.UtcNow, "tester", null, null);
            receipt.AddLine(new ReceiptLine(
                receipt.Id, 1, poLineId, null, "Widgets", 12m, "EA", null, null, false, null, null));

            purchasing.Receipts.Add(receipt);
            await purchasing.SaveChangesAsync();

            receipt.Post();
            await purchasing.SaveChangesAsync();
        }

        var match = await ExecuteInTransactionAsync(async sp =>
        {
            var ap = sp.GetRequiredService<ApDbContext>();
            return await ap.GoodsReceiptMatches
                .Where(m => m.PurchaseOrderLineId == poLineId)
                .FirstOrDefaultAsync();
        });

        match.Should().NotBeNull();
        match!.QuantityReceived.Should().Be(12m);
        match.OverReceiptFlag.Should().BeTrue("received qty 12 exceeds ordered 10 by more than 5% tolerance");
    }
}
