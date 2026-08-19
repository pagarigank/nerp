// <copyright file="UnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly GlDbContext _context;
    private IRepository<JournalBatch>? _journalBatches;
    private IRepository<JournalEntryLine>? _journalEntryLines;
    private IRepository<RecurringTemplate>? _recurringTemplates;
    private IRepository<RecurringTemplateLine>? _recurringTemplateLines;
    private IRepository<AllocationRule>? _allocationRules;
    private IRepository<AllocationRuleLine>? _allocationRuleLines;
    private IRepository<Budget>? _budgets;
    private IRepository<BudgetLine>? _budgetLines;
    private bool _disposed;

    public UnitOfWork(GlDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IRepository<JournalBatch> JournalBatches => _journalBatches ??= new Repository<JournalBatch>(_context);
    public IRepository<JournalEntryLine> JournalEntryLines => _journalEntryLines ??= new Repository<JournalEntryLine>(_context);
    public IRepository<RecurringTemplate> RecurringTemplates => _recurringTemplates ??= new Repository<RecurringTemplate>(_context);
    public IRepository<RecurringTemplateLine> RecurringTemplateLines => _recurringTemplateLines ??= new Repository<RecurringTemplateLine>(_context);
    public IRepository<AllocationRule> AllocationRules => _allocationRules ??= new Repository<AllocationRule>(_context);
    public IRepository<AllocationRuleLine> AllocationRuleLines => _allocationRuleLines ??= new Repository<AllocationRuleLine>(_context);
    public IRepository<Budget> Budgets => _budgets ??= new Repository<Budget>(_context);
    public IRepository<BudgetLine> BudgetLines => _budgetLines ??= new Repository<BudgetLine>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
