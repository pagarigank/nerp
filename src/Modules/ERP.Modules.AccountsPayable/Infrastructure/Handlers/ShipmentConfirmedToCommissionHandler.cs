// <copyright file="ShipmentConfirmedToCommissionHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Domain.Events;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="ShipmentConfirmedEvent"/> (raised when a sales shipment is confirmed) and
/// accrues sales-rep commission — the Sales -&gt; AP integration. For each shipment line it looks
/// up the originating sales order's sales rep (via OmDbContext), computes commission
/// (line extended value x rep commission rate), records a <see cref="CommissionAccrual"/> and
/// creates an AP voucher (Commission Expense debit / AP control credit) that posts to the General
/// Ledger. Commission is only accrued when the rep is linked to an AP vendor.
/// </summary>
public sealed class ShipmentConfirmedToCommissionHandler : IDomainEventHandler<ShipmentConfirmedEvent>
{
    private const string ApControlAccountNumber = "2000";
    private const string CommissionExpenseAccountNumber = "6200";

    private readonly ApDbContext _apContext;
    private readonly OmDbContext _omContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;
    private readonly IPeriodService _periodService;

    public ShipmentConfirmedToCommissionHandler(
        ApDbContext apContext,
        OmDbContext omContext,
        PlatformDbContext platformContext,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser,
        IPeriodService periodService)
    {
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _omContext = omContext ?? throw new ArgumentNullException(nameof(omContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
    }

    public async Task HandleAsync(ShipmentConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Lines.Count == 0 || domainEvent.SalesOrderId is null)
            return;

        var order = await _omContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == domainEvent.SalesOrderId, cancellationToken);
        if (order is null || string.IsNullOrWhiteSpace(order.SalesRepId))
            return;

        if (!Guid.TryParse(order.SalesRepId, out var salesRepId))
            return;

        var rep = await _omContext.SalesReps
            .FirstOrDefaultAsync(r => r.Id == salesRepId, cancellationToken);
        if (rep is null || rep.VendorId is null)
            return; // Rep not linked to an AP vendor: no commission payable target.

        var vendor = await _apContext.Vendors.FirstOrDefaultAsync(v => v.Id == rep.VendorId, cancellationToken);
        if (vendor is null)
            return;

        var commissionExpense = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == domainEvent.CompanyId && a.AccountNumber == CommissionExpenseAccountNumber, cancellationToken);
        var apControl = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == domainEvent.CompanyId && a.AccountNumber == ApControlAccountNumber, cancellationToken);
        if (commissionExpense is null || apControl is null)
            return; // Chart of accounts not seeded for commission/AP control; skip accrual.

        var period = await _periodService.GetCurrentPeriodAsync(domainEvent.CompanyId, cancellationToken);
        var postingDate = DateTimeOffset.UtcNow;

        decimal totalCommission = 0m;
        foreach (var line in domainEvent.Lines)
        {
            var baseAmount = line.Quantity * line.UnitPrice;
            var commission = baseAmount * (rep.CommissionRate / 100m);
            if (commission <= 0m)
                continue;

            totalCommission += commission;
            var accrual = new CommissionAccrual(
                domainEvent.CompanyId,
                rep.Id,
                rep.VendorId,
                domainEvent.ShipmentId,
                domainEvent.ShipmentNumber,
                order.Id,
                order.OrderNumber,
                domainEvent.CustomerId,
                baseAmount,
                rep.CommissionRate,
                commission,
                null);
            _apContext.CommissionAccruals.Add(accrual);
        }

        if (totalCommission <= 0m)
            return;

        var batch = new VoucherBatch(
            domainEvent.CompanyId,
            $"COMM-{domainEvent.ShipmentNumber}",
            $"Commission accrual for shipment {domainEvent.ShipmentNumber}",
            postingDate,
            period?.Id ?? Guid.Empty);

        var voucher = batch.AddVoucher(
            vendor.Id,
            VoucherType.Invoice,
            $"COMM-{domainEvent.ShipmentNumber}",
            postingDate,
            postingDate.AddDays(30),
            totalCommission,
            0m,
            $"Commission for {rep.Name} on shipment {domainEvent.ShipmentNumber}",
            null);

        voucher.AddDistribution(commissionExpense.Id, debit: totalCommission, credit: null, projectId: null, taskId: null);
        voucher.AddDistribution(apControl.Id, debit: null, credit: totalCommission, projectId: null, taskId: null);
        batch.Release();
        _apContext.VoucherBatches.Add(batch);

        // Link the accrual records to the voucher that pays them.
        foreach (var accrual in _apContext.CommissionAccruals.Local.Where(a => a.ShipmentId == domainEvent.ShipmentId && a.VoucherId is null))
        {
            accrual.SetVoucherId(voucher.Id);
        }

        await _apContext.SaveChangesAsync(cancellationToken);

        var lines = new List<PostingLine>
        {
            new PostingLine
            {
                AccountId = commissionExpense.Id,
                Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
                Debit = totalCommission,
                Credit = 0m,
                Currency = "USD",
            },
            new PostingLine
            {
                AccountId = apControl.Id,
                Segments = ERP.Shared.Kernel.Posting.AccountKey.Create(),
                Debit = 0m,
                Credit = totalCommission,
                Currency = "USD",
            },
        };

        var postingEvent = CanonicalPostingEvent.Create(
            "AP",
            $"COMM-{domainEvent.ShipmentNumber}",
            domainEvent.CompanyId,
            period?.Id ?? Guid.Empty,
            domainEvent.CompanyId.ToString(),
            period?.Id.ToString() ?? string.Empty,
            postingDate,
            lines,
            PostingMetadata.Create(_currentUser.UserId ?? "system", Guid.NewGuid(), customerId: null, projectId: null));

        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
    }
}
