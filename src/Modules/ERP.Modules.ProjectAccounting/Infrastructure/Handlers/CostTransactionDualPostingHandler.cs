// <copyright file="CostTransactionDualPostingHandler.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Infrastructure.Handlers;

/// <summary>
/// Consumes <see cref="ProjectCostPostedEvent"/> (raised by the Cost Transaction
/// controller when a cost is posted to a project) and dual-posts it to the
/// General Ledger through the canonical posting contract (architecture.md §5.1).
///
/// The project ledger is the system of record for job costs; the GL is the
/// system of record for financials. This handler keeps both in lock-step:
///   Dr &lt;job-cost GL account for the cost category&gt;  (WIP / asset)
///   Cr 2300 Accrued Job Costs                                (liability)
/// with PROJECT/TASK segments so the GL can be reconciled to the project ledger
/// (the "Project-to-GL reconciliation check" gate). A per-company
/// <see cref="ProjectCostCategoryMapping"/> drives which GL account receives the
/// debit; if no mapping is configured the cost-category default account is used.
/// </summary>
public sealed class CostTransactionDualPostingHandler : IDomainEventHandler<ProjectCostPostedEvent>
{
    // Cost-category default GL accounts (used only when no per-company mapping exists).
    private const string DefaultLaborAccount = "6000";   // Salaries & Wages
    private const string DefaultMaterialsAccount = "5000"; // Cost of Goods Sold
    private const string DefaultSubcontractAccount = "6100"; // Rent (reused as subcontract expense bucket)
    private const string DefaultEquipmentAccount = "1500";  // Equipment
    private const string DefaultOverheadAccount = "6200";   // Utilities (overhead bucket)
    private const string DefaultOtherAccount = "7000";      // Other Expense
    private const string AccruedJobCostsNumber = "2300";    // new job-costing liability account

    private readonly ProjDbContext _projContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;

    public CostTransactionDualPostingHandler(
        ProjDbContext projContext,
        PlatformDbContext platformContext,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser)
    {
        _projContext = projContext ?? throw new ArgumentNullException(nameof(projContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task HandleAsync(ProjectCostPostedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Amount == 0m)
            return;

        // Resolve the GL account for this cost category (per-company mapping wins).
        var glAccountId = await ResolveGlAccountAsync(domainEvent.CompanyId, domainEvent.CostCategory, cancellationToken);
        var accruedAccountId = await ResolveAccountAsync(domainEvent.CompanyId, AccruedJobCostsNumber, cancellationToken);

        var segments = ERP.Shared.Kernel.Posting.AccountKey.Create()
            .WithSegment("PROJECT", domainEvent.ProjectId.ToString())
            .WithSegment("TASK", domainEvent.TaskId.ToString());

        // A project cost may be negative (a credit/reversal, e.g. a returned
        // inventory issue). GL posting lines must be non-negative, so orient the
        // legs by the sign of the amount: a positive cost debits the job-cost
        // account and credits the accrued-job-costs liability; a negative cost
        // flips both legs.
        var amount = domainEvent.Amount;
        var jobCostDebit = amount >= 0m ? amount : 0m;
        var jobCostCredit = amount < 0m ? -amount : 0m;
        var accruedDebit = amount < 0m ? -amount : 0m;
        var accruedCredit = amount >= 0m ? amount : 0m;

        var lines = new List<PostingLine>
        {
            new PostingLine
            {
                AccountId = glAccountId,
                Segments = segments,
                Debit = jobCostDebit,
                Credit = jobCostCredit,
                Currency = "USD"
            },
            new PostingLine
            {
                AccountId = accruedAccountId,
                Segments = segments,
                Debit = accruedDebit,
                Credit = accruedCredit,
                Currency = "USD"
            }
        };

        var period = await ResolveFiscalPeriodAsync(domainEvent.CompanyId, DateTime.UtcNow, cancellationToken);
        var postedBy = _currentUser.UserId ?? "system";

        var postingEvent = CanonicalPostingEvent.Create(
            "PROJ",
            $"PROJ-COST-{domainEvent.CostTransactionId:N}",
            domainEvent.CompanyId,
            period?.Id ?? domainEvent.CompanyId,
            domainEvent.CompanyId.ToString(),
            (period?.Id ?? domainEvent.CompanyId).ToString(),
            DateTime.UtcNow,
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid(), projectId: domainEvent.ProjectId.ToString()));

        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
    }

    private async Task<Guid> ResolveGlAccountAsync(Guid companyId, string costCategory, CancellationToken cancellationToken)
    {
        // The cost category arrives as a string from the raised event; parse it to
        // the enum so the query compares the mapped enum column directly (EF
        // cannot translate Enum.ToString()).
        if (!Enum.TryParse<CostCategory>(costCategory, true, out var category))
            category = CostCategory.Other;

        var mapping = await _projContext.ProjectCostCategoryMappings
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.CostCategory == category, cancellationToken);

        if (mapping is not null)
            return mapping.GlAccountId;

        var fallback = category switch
        {
            CostCategory.Labor => DefaultLaborAccount,
            CostCategory.Materials => DefaultMaterialsAccount,
            CostCategory.Subcontract => DefaultSubcontractAccount,
            CostCategory.Equipment => DefaultEquipmentAccount,
            CostCategory.Overhead => DefaultOverheadAccount,
            _ => DefaultOtherAccount
        };

        return await ResolveAccountAsync(companyId, fallback, cancellationToken);
    }

    private async Task<Guid> ResolveAccountAsync(Guid companyId, string accountNumber, CancellationToken cancellationToken)
    {
        var account = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountNumber == accountNumber, cancellationToken);

        if (account is null)
        {
            throw new InvalidOperationException(
                $"GL account '{accountNumber}' for company {companyId} was not found. " +
                "Seed a chart-of-accounts entry for the job-costing accounts before posting project costs.");
        }

        return account.Id;
    }

    private async Task<FiscalPeriod?> ResolveFiscalPeriodAsync(
        Guid companyId, DateTime transactionDate, CancellationToken cancellationToken)
    {
        var date = new DateTimeOffset(transactionDate);
        return await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.StartDate <= date && p.EndDate >= date)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
