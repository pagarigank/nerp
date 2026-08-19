// <copyright file="ConsolidationRun.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

public class ConsolidationRun : AuditableAggregateRoot
{
    protected ConsolidationRun() { }

    public ConsolidationRun(
        Guid parentCompanyId,
        int fiscalYear,
        int fiscalPeriod,
        string description,
        DateTimeOffset consolidationDate)
        : base(Guid.NewGuid())
    {
        ParentCompanyId = parentCompanyId;
        FiscalYear = fiscalYear;
        FiscalPeriod = fiscalPeriod;
        Description = description ?? string.Empty;
        ConsolidationDate = consolidationDate;
        Status = ConsolidationRunStatus.Draft;
    }

    public Guid ParentCompanyId { get; private set; }

    public int FiscalYear { get; private set; }

    public int FiscalPeriod { get; private set; }

    public Guid FiscalPeriodId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset ConsolidationDate { get; private set; }

    public ConsolidationRunStatus Status { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void SetFiscalPeriodId(Guid fiscalPeriodId)
    {
        FiscalPeriodId = fiscalPeriodId;
    }

    public void StartProcessing()
    {
        if (Status != ConsolidationRunStatus.Draft)
            throw new InvalidOperationException("Only a Draft consolidation run can be started.");

        Status = ConsolidationRunStatus.Processing;
    }

    public void Complete()
    {
        if (Status != ConsolidationRunStatus.Processing)
            throw new InvalidOperationException("Only a Processing consolidation run can be completed.");

        Status = ConsolidationRunStatus.Completed;
    }

    public void Fail(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required.", nameof(errorMessage));

        Status = ConsolidationRunStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void UpdateDescription(string description)
    {
        if (Status != ConsolidationRunStatus.Draft)
            throw new InvalidOperationException("Cannot modify a consolidation run that is not in Draft status.");

        Description = description ?? string.Empty;
    }
}

public enum ConsolidationRunStatus
{
    Draft = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
}