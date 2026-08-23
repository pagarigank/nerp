// <copyright file="VoucherPostedToProjectHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="VoucherPostedEvent"/> raised when an AP voucher batch posts and
/// records a <see cref="CostTransaction"/> in the project ledger for every project-tagged
/// distribution line, mirroring InventoryPostedToProjectHandler's mapping. The event's
/// category string is honored when supplied; vendor-invoice cost otherwise defaults to
/// Materials. Raises <see cref="ProjectCostPostedEvent"/> so the dual-posting handler
/// flows the cost to GL via the per-company ProjectCostCategoryMapping account overlay.
/// </summary>
public sealed class VoucherPostedToProjectHandler : IDomainEventHandler<VoucherPostedEvent>
{
    private readonly ProjDbContext _projContext;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public VoucherPostedToProjectHandler(
        ProjDbContext projContext,
        IDomainEventDispatcher eventDispatcher)
    {
        _projContext = projContext ?? throw new ArgumentNullException(nameof(projContext));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
    }

    public async Task HandleAsync(VoucherPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var alreadyPosted = await _projContext.CostTransactions
            .AnyAsync(t => t.SourceId == domainEvent.VoucherId && t.TransactionType == CostTransactionType.ApVoucher, cancellationToken);
        if (alreadyPosted)
        {
            return;
        }

        foreach (var line in domainEvent.Lines)
        {
            if (line.ProjectId is null || line.Amount == 0m)
            {
                continue;
            }

            var project = await _projContext.Projects
                .FirstOrDefaultAsync(p => p.Id == line.ProjectId.Value, cancellationToken);
            if (project is null)
            {
                continue;
            }

            var task = line.TaskId.HasValue
                ? await _projContext.ProjectTasks
                    .FirstOrDefaultAsync(t => t.Id == line.TaskId.Value && t.ProjectId == project.Id, cancellationToken)
                : null;
            task ??= await _projContext.ProjectTasks
                .Where(t => t.ProjectId == project.Id)
                .OrderBy(t => t.TaskCode)
                .FirstOrDefaultAsync(cancellationToken);

            if (task is null)
            {
                continue;
            }

            var category = Enum.TryParse<CostCategory>(line.Category, true, out var parsed)
                ? parsed
                : CostCategory.Materials;

            var txn = new CostTransaction(
                domainEvent.CompanyId,
                project.Id,
                task.Id,
                category,
                CostTransactionType.ApVoucher,
                line.Amount,
                0m,
                $"AP voucher {domainEvent.VoucherNumber} (vendor invoice)",
                domainEvent.VoucherId,
                $"VCH-{domainEvent.VoucherId:N}",
                isBillable: true,
                vendorId: domainEvent.VendorId,
                employeeId: null);

            _projContext.CostTransactions.Add(txn);
            project.RecalculateCosts();

            await _projContext.SaveChangesAsync(cancellationToken);

            await _eventDispatcher.DispatchAsync(new ProjectCostPostedEvent(
                txn.Id, txn.ProjectId, txn.TaskId, txn.Category.ToString(), txn.Amount, txn.CompanyId), cancellationToken);
        }
    }
}
