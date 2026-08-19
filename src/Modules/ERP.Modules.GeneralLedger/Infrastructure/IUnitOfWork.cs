// <copyright file="IUnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.GeneralLedger.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    IRepository<Domain.Entities.JournalBatch> JournalBatches { get; }
    IRepository<Domain.Entities.JournalEntryLine> JournalEntryLines { get; }
    IRepository<Domain.Entities.RecurringTemplate> RecurringTemplates { get; }
    IRepository<Domain.Entities.RecurringTemplateLine> RecurringTemplateLines { get; }
    IRepository<Domain.Entities.AllocationRule> AllocationRules { get; }
    IRepository<Domain.Entities.AllocationRuleLine> AllocationRuleLines { get; }
    IRepository<Domain.Entities.Budget> Budgets { get; }
    IRepository<Domain.Entities.BudgetLine> BudgetLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
