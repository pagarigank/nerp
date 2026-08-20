// <copyright file="PayrollPostedToProjectHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="LaborPostedToProjectEvent"/> (raised when a Payroll timesheet is approved)
/// and posts the labor cost to the project ledger as a <see cref="CostTransaction"/>, then raises
/// <see cref="ProjectCostPostedEvent"/> so the dual-posting handler flows it to GL. This closes the
/// Phase 10 / Phase 11 wiring dependency (payroll labor → project cost → GL).
/// </summary>
public sealed class PayrollPostedToProjectHandler : IDomainEventHandler<LaborPostedToProjectEvent>
{
    private readonly ProjDbContext _projContext;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PayrollPostedToProjectHandler(ProjDbContext projContext, IDomainEventDispatcher eventDispatcher)
    {
        _projContext = projContext ?? throw new ArgumentNullException(nameof(projContext));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
    }

    public async Task HandleAsync(LaborPostedToProjectEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var line in domainEvent.Lines)
        {
            if (line.ProjectId is null)
                continue; // non-project (overhead) labor is not job-costed

            var project = await _projContext.Projects
                .FirstOrDefaultAsync(p => p.Id == line.ProjectId.Value, cancellationToken);
            if (project is null)
                continue;

            var txn = new CostTransaction(
                companyId: domainEvent.CompanyId,
                projectId: line.ProjectId.Value,
                taskId: line.TaskId ?? Guid.Empty,
                category: CostCategory.Labor,
                transactionType: CostTransactionType.PayrollLabor,
                amount: line.Amount,
                hours: line.Hours,
                description: $"Payroll labor {domainEvent.WeekEnding:yyyy-MM-dd} (timesheet {domainEvent.TimesheetId})",
                sourceId: domainEvent.TimesheetId,
                sourceReference: domainEvent.TimesheetId.ToString(),
                isBillable: line.IsBillable,
                vendorId: null,
                employeeId: domainEvent.EmployeeId);

            _projContext.CostTransactions.Add(txn);
            await _projContext.SaveChangesAsync(cancellationToken);

            // Raise the project-cost event so the dual-posting handler posts to GL + updates project CostsToDate.
            await _eventDispatcher.DispatchAsync(new ProjectCostPostedEvent(
                txn.Id, txn.ProjectId, txn.TaskId, txn.Category.ToString(), txn.Amount, txn.CompanyId), cancellationToken);
        }
    }
}
