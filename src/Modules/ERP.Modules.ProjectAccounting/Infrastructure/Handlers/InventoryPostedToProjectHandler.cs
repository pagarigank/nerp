// <copyright file="InventoryPostedToProjectHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Inventory.Domain.Events;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="InventoryTransactionPostedEvent"/> raised when an inventory
/// transaction is committed, and — when the movement is an <c>Issue</c> against a
/// specific project (the item was issued to a job) — records the material cost as
/// a <see cref="CostTransaction"/> in the project ledger. This closes the
/// previously-unwired "item issued to project posts cost to project ledger"
/// integration (spec §7.3 / Phase 10 cost item-issue gap) and feeds the project's
/// CostsToDate, driving WIP, EAC and profitability. The GL leg for the same issue
/// is handled separately by <c>InventoryPostedToGlHandler</c>; this handler raises
/// <see cref="ProjectCostPostedEvent"/> so the project-cost dual-posting handler
/// posts the matching GL entry (architecture.md §5.1).
///
/// NOTE: only the event payload is used (no InventoryDbContext dependency) to keep
/// the Project Accounting module decoupled from the Inventory module.
/// </summary>
public sealed class InventoryPostedToProjectHandler : IDomainEventHandler<InventoryTransactionPostedEvent>
{
    private readonly ProjDbContext _projContext;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public InventoryPostedToProjectHandler(
        ProjDbContext projContext,
        IDomainEventDispatcher eventDispatcher)
    {
        _projContext = projContext ?? throw new ArgumentNullException(nameof(projContext));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
    }

    public async Task HandleAsync(InventoryTransactionPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Only inventory issuances against a specific project create project costs.
        if (domainEvent.ProjectId is null)
            return;
        if (!string.Equals(domainEvent.TransactionType, "Issue", StringComparison.OrdinalIgnoreCase))
            return;
        if (domainEvent.ExtendedCost == 0m)
            return;

        var project = await _projContext.Projects
            .FirstOrDefaultAsync(p => p.Id == domainEvent.ProjectId.Value, cancellationToken);

        if (project is null)
            return; // Issue references a project absent from the project ledger; nothing to post.

        // Attach the cost to the first WBS task when the inventory issue does not
        // name a finer-grained task.
        var task = await _projContext.ProjectTasks
            .Where(t => t.ProjectId == domainEvent.ProjectId.Value)
            .OrderBy(t => t.TaskCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
            return; // Cannot post a cost without a WBS task to attach it to.

        var cost = new CostTransaction(
            project.CompanyId,
            project.Id,
            task.Id,
            CostCategory.Materials,
            CostTransactionType.InventoryIssue,
            domainEvent.ExtendedCost,
            0m,
            $"Inventory issue {domainEvent.TransactionId:N} to project",
            domainEvent.TransactionId,
            $"INV-{domainEvent.TransactionId:N}",
            isBillable: true,
            vendorId: null,
            employeeId: null);

        _projContext.CostTransactions.Add(cost);
        project.RecalculateCosts();

        await _projContext.SaveChangesAsync(cancellationToken);

        // Raise the project-cost event so the GL dual-posting handler runs.
        await _eventDispatcher.DispatchAsync(
            new ProjectCostPostedEvent(
                cost.Id,
                project.Id,
                task.Id,
                CostCategory.Materials.ToString(),
                domainEvent.ExtendedCost,
                project.CompanyId),
            cancellationToken);
    }
}
