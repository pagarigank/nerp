// <copyright file="RecurringTemplate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class RecurringTemplate : AuditableAggregateRoot
{
    private readonly List<RecurringTemplateLine> _lines = [];

    protected RecurringTemplate() { }

    public RecurringTemplate(
        Guid companyId,
        string name,
        string description,
        RecurringFrequency frequency,
        DateTimeOffset nextRunDate,
        bool isActive) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));

        CompanyId = companyId;
        Name = name;
        Description = description ?? string.Empty;
        Frequency = frequency;
        NextRunDate = nextRunDate;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public RecurringFrequency Frequency { get; private set; }

    public DateTimeOffset NextRunDate { get; private set; }

    public DateTimeOffset? LastRunDate { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<RecurringTemplateLine> Lines => _lines.AsReadOnly();

    public void AddLine(Guid accountId, decimal? fixedDebit, decimal? fixedCredit, decimal? variablePct, string? reference = null)
    {
        if (!fixedDebit.HasValue && !fixedCredit.HasValue && !variablePct.HasValue)
            throw new ArgumentException("Line must specify a fixed amount or variable percentage.");

        if (fixedDebit.HasValue && fixedCredit.HasValue)
            throw new ArgumentException("Line cannot have both debit and credit fixed amounts.");

        var line = new RecurringTemplateLine(Id, accountId, fixedDebit ?? 0, fixedCredit ?? 0, variablePct, reference);
        _lines.Add(line);
    }

    public JournalBatch GenerateBatch(string batchNumber, Guid fiscalPeriodId, DateTimeOffset postingDate)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot generate from an inactive template.");

        var batch = new JournalBatch(CompanyId, batchNumber, $"Recurring: {Name}", postingDate, fiscalPeriodId);

        foreach (var line in _lines)
        {
            if (line.VariablePct.HasValue)
            {
                continue;
            }

            batch.AddLine(line.AccountId, line.FixedDebit, line.FixedCredit, line.Reference ?? Name);
        }

        LastRunDate = postingDate;
        UpdateNextRunDate();

        return batch;
    }

    public void UpdateNextRunDate()
    {
        NextRunDate = Frequency switch
        {
            RecurringFrequency.Monthly => NextRunDate.AddMonths(1),
            RecurringFrequency.Quarterly => NextRunDate.AddMonths(3),
            RecurringFrequency.SemiAnnually => NextRunDate.AddMonths(6),
            RecurringFrequency.Annually => NextRunDate.AddYears(1),
            _ => NextRunDate.AddMonths(1),
        };
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void Update(string name, string description, RecurringFrequency frequency, DateTimeOffset nextRunDate, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.", nameof(name));

        Name = name;
        Description = description ?? string.Empty;
        Frequency = frequency;
        NextRunDate = nextRunDate;
        IsActive = isActive;
    }
}

public enum RecurringFrequency
{
    Monthly = 0,
    Quarterly = 1,
    SemiAnnually = 2,
    Annually = 3,
    Custom = 4,
}
