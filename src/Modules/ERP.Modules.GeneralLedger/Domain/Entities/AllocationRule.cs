// <copyright file="AllocationRule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class AllocationRule : AuditableAggregateRoot
{
    private readonly List<AllocationRuleLine> _lines = [];

    protected AllocationRule() { }

    public AllocationRule(
        Guid companyId,
        string name,
        string description,
        Guid sourceAccountId,
        AllocationMethod method,
        bool isActive) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name is required.", nameof(name));

        CompanyId = companyId;
        Name = name;
        Description = description ?? string.Empty;
        SourceAccountId = sourceAccountId;
        Method = method;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid SourceAccountId { get; private set; }

    public AllocationMethod Method { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<AllocationRuleLine> Lines => _lines.AsReadOnly();

    public void AddLine(Guid targetAccountId, decimal percentage, decimal? fixedAmount = null, string? reference = null)
    {
        if (percentage <= 0)
            throw new ArgumentException("Percentage must be positive.", nameof(percentage));

        if (Method == AllocationMethod.Percentage)
        {
            var totalPct = _lines.Sum(l => l.Percentage) + percentage;
            if (totalPct > 100)
                throw new InvalidOperationException($"Total percentage would exceed 100% ({totalPct}%).");
        }

        var line = new AllocationRuleLine(Id, targetAccountId, percentage, fixedAmount, reference);
        _lines.Add(line);
    }

    public JournalBatch ExecuteAllocation(
        string batchNumber,
        decimal sourceAmount,
        Guid fiscalPeriodId,
        DateTimeOffset postingDate)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot execute an inactive allocation rule.");

        if (sourceAmount <= 0)
            throw new ArgumentException("Source amount must be positive.", nameof(sourceAmount));

        var batch = new JournalBatch(CompanyId, batchNumber, $"Allocation: {Name}", postingDate, fiscalPeriodId);

        batch.AddLine(SourceAccountId, 0, sourceAmount, $"Allocation source: {Name}");

        foreach (var line in _lines)
        {
            var amount = Method switch
            {
                AllocationMethod.Percentage => Math.Round(sourceAmount * line.Percentage / 100, 2),
                AllocationMethod.FixedAmount => line.FixedAmount ?? 0,
                AllocationMethod.Equally => Math.Round(sourceAmount / _lines.Count, 2),
                _ => 0,
            };

            if (amount <= 0)
                continue;

            batch.AddLine(line.TargetAccountId, amount, 0, line.Reference ?? $"Allocation: {Name}");
        }

        return batch;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void Update(string name, string description, Guid sourceAccountId, AllocationMethod method, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name is required.", nameof(name));

        Name = name;
        Description = description ?? string.Empty;
        SourceAccountId = sourceAccountId;
        Method = method;
        IsActive = isActive;
    }
}

public enum AllocationMethod
{
    Percentage = 0,
    FixedAmount = 1,
    Equally = 2,
}
