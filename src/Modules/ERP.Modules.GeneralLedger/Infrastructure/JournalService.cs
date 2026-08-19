// <copyright file="JournalService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.GeneralLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.GeneralLedger.Infrastructure;

public interface IJournalService
{
    Task<JournalBatch> CreateBatchAsync(Guid companyId, string batchNumber, string description, DateTimeOffset postingDate, Guid fiscalPeriodId, CancellationToken cancellationToken = default);

    Task<JournalBatch> PostBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<JournalBatch> ReverseBatchAsync(Guid batchId, string reason, CancellationToken cancellationToken = default);

    Task<JournalBatch> GenerateFromRecurringAsync(Guid templateId, string batchNumber, Guid fiscalPeriodId, DateTimeOffset postingDate, CancellationToken cancellationToken = default);

    Task<JournalBatch> ExecuteAllocationAsync(Guid ruleId, string batchNumber, decimal sourceAmount, Guid fiscalPeriodId, DateTimeOffset postingDate, CancellationToken cancellationToken = default);

    Task<bool> CanPostInPeriodAsync(Guid companyId, Guid fiscalPeriodId, CancellationToken cancellationToken = default);

    // Recurring Template CRUD
    Task<RecurringTemplate> CreateRecurringTemplateAsync(Guid companyId, string name, string description, RecurringFrequency frequency, DateTimeOffset nextRunDate, bool isActive, CancellationToken cancellationToken = default);

    Task<RecurringTemplate> UpdateRecurringTemplateAsync(Guid templateId, string name, string description, RecurringFrequency frequency, DateTimeOffset nextRunDate, bool isActive, CancellationToken cancellationToken = default);

    Task<RecurringTemplate> AddRecurringTemplateLineAsync(Guid templateId, Guid accountId, decimal? fixedDebit, decimal? fixedCredit, decimal? variablePct, string? reference, CancellationToken cancellationToken = default);

    Task DeleteRecurringTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<RecurringTemplate?> GetRecurringTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringTemplate>> GetRecurringTemplatesAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default);

    // Allocation Rule CRUD
    Task<AllocationRule> CreateAllocationRuleAsync(Guid companyId, string name, string description, Guid sourceAccountId, AllocationMethod method, bool isActive, CancellationToken cancellationToken = default);

    Task<AllocationRule> UpdateAllocationRuleAsync(Guid ruleId, string name, string description, Guid sourceAccountId, AllocationMethod method, bool isActive, CancellationToken cancellationToken = default);

    Task<AllocationRule> AddAllocationRuleLineAsync(Guid ruleId, Guid targetAccountId, decimal percentage, decimal? fixedAmount, string? reference, CancellationToken cancellationToken = default);

    Task DeleteAllocationRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<AllocationRule?> GetAllocationRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default);
}

public class JournalService : IJournalService
{
    private readonly GlDbContext _context;

    public JournalService(GlDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<JournalBatch> CreateBatchAsync(
        Guid companyId,
        string batchNumber,
        string description,
        DateTimeOffset postingDate,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken = default)
    {
        var batch = new JournalBatch(companyId, batchNumber, description, postingDate, fiscalPeriodId);
        _context.JournalBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<JournalBatch> PostBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.JournalBatches
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Journal batch {batchId} not found.");

        batch.Release();

        batch.Post();

        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<JournalBatch> ReverseBatchAsync(Guid batchId, string reason, CancellationToken cancellationToken = default)
    {
        var batch = await _context.JournalBatches
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Journal batch {batchId} not found.");

        var reversal = batch.Reverse(reason);

        _context.JournalBatches.Add(reversal);
        await _context.SaveChangesAsync(cancellationToken);
        return reversal;
    }

    public async Task<JournalBatch> GenerateFromRecurringAsync(
        Guid templateId,
        string batchNumber,
        Guid fiscalPeriodId,
        DateTimeOffset postingDate,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Recurring template {templateId} not found.");

        var batch = template.GenerateBatch(batchNumber, fiscalPeriodId, postingDate);

        _context.JournalBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<JournalBatch> ExecuteAllocationAsync(
        Guid ruleId,
        string batchNumber,
        decimal sourceAmount,
        Guid fiscalPeriodId,
        DateTimeOffset postingDate,
        CancellationToken cancellationToken = default)
    {
        var rule = await _context.AllocationRules
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken)
            ?? throw new InvalidOperationException($"Allocation rule {ruleId} not found.");

        var batch = rule.ExecuteAllocation(batchNumber, sourceAmount, fiscalPeriodId, postingDate);

        _context.JournalBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<bool> CanPostInPeriodAsync(Guid companyId, Guid fiscalPeriodId, CancellationToken cancellationToken = default)
    {
        var openBatchCount = await _context.JournalBatches
            .CountAsync(b => b.CompanyId == companyId
                && b.FiscalPeriodId == fiscalPeriodId
                && b.Status == JournalBatchStatus.Draft, cancellationToken);

        return openBatchCount >= 0;
    }

    // Recurring Template CRUD
    public async Task<RecurringTemplate> CreateRecurringTemplateAsync(
        Guid companyId,
        string name,
        string description,
        RecurringFrequency frequency,
        DateTimeOffset nextRunDate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var template = new RecurringTemplate(companyId, name, description, frequency, nextRunDate, isActive);
        _context.RecurringTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<RecurringTemplate> UpdateRecurringTemplateAsync(
        Guid templateId,
        string name,
        string description,
        RecurringFrequency frequency,
        DateTimeOffset nextRunDate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Recurring template {templateId} not found.");

        template.Update(name, description, frequency, nextRunDate, isActive);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<RecurringTemplate> AddRecurringTemplateLineAsync(
        Guid templateId,
        Guid accountId,
        decimal? fixedDebit,
        decimal? fixedCredit,
        decimal? variablePct,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Recurring template {templateId} not found.");

        template.AddLine(accountId, fixedDebit, fixedCredit, variablePct, reference);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task DeleteRecurringTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Recurring template {templateId} not found.");

        _context.RecurringTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RecurringTemplate?> GetRecurringTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.RecurringTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }

    public async Task<IReadOnlyList<RecurringTemplate>> GetRecurringTemplatesAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RecurringTemplates
            .Include(t => t.Lines)
            .Where(t => t.CompanyId == companyId);

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // Allocation Rule CRUD
    public async Task<AllocationRule> CreateAllocationRuleAsync(
        Guid companyId,
        string name,
        string description,
        Guid sourceAccountId,
        AllocationMethod method,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var rule = new AllocationRule(companyId, name, description, sourceAccountId, method, isActive);
        _context.AllocationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<AllocationRule> UpdateAllocationRuleAsync(
        Guid ruleId,
        string name,
        string description,
        Guid sourceAccountId,
        AllocationMethod method,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var rule = await _context.AllocationRules
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken)
            ?? throw new InvalidOperationException($"Allocation rule {ruleId} not found.");

        rule.Update(name, description, sourceAccountId, method, isActive);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<AllocationRule> AddAllocationRuleLineAsync(
        Guid ruleId,
        Guid targetAccountId,
        decimal percentage,
        decimal? fixedAmount,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        var rule = await _context.AllocationRules
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken)
            ?? throw new InvalidOperationException($"Allocation rule {ruleId} not found.");

        rule.AddLine(targetAccountId, percentage, fixedAmount, reference);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task DeleteAllocationRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.AllocationRules
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken)
            ?? throw new InvalidOperationException($"Allocation rule {ruleId} not found.");

        _context.AllocationRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AllocationRule?> GetAllocationRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        return await _context.AllocationRules
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);
    }

    public async Task<IReadOnlyList<AllocationRule>> GetAllocationRulesAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AllocationRules
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId);

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
