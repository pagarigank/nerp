// <copyright file="GlPhase2Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.GeneralLedger.Domain.Entities;

/// <summary>
/// Records a fiscal-year (income-statement) close that rolls net income into retained earnings
/// and locks the fiscal year. Reopen-by-exception is supported via the Status.
/// </summary>
public class YearEndCloseRun : AuditableAggregateRoot
{
    protected YearEndCloseRun() { }

    public YearEndCloseRun(
        Guid companyId,
        Guid fiscalYearId,
        Guid retainedEarningsAccountId,
        DateTimeOffset closedOn,
        string closedBy,
        decimal totalRevenue,
        decimal totalExpense,
        decimal retainedEarningsAmount)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        FiscalYearId = fiscalYearId;
        RetainedEarningsAccountId = retainedEarningsAccountId;
        ClosedOn = closedOn;
        ClosedBy = closedBy;
        TotalRevenue = totalRevenue;
        TotalExpense = totalExpense;
        RetainedEarningsAmount = retainedEarningsAmount;
        Status = YearEndCloseStatus.Completed;
    }

    public Guid CompanyId { get; private set; }
    public Guid FiscalYearId { get; private set; }
    public Guid RetainedEarningsAccountId { get; private set; }
    public DateTimeOffset ClosedOn { get; private set; }
    public string ClosedBy { get; private set; } = string.Empty;
    public decimal TotalRevenue { get; private set; }
    public decimal TotalExpense { get; private set; }
    public decimal RetainedEarningsAmount { get; private set; }
    public YearEndCloseStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void Fail(string errorMessage)
    {
        Status = YearEndCloseStatus.Failed;
        ErrorMessage = errorMessage;
    }
}

public enum YearEndCloseStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Reopened = 3
}

/// <summary>
/// A posting that failed validation (invalid segment combo, unbalanced, closed period, inactive
/// account) lands here instead of erroring out, with an actionable reason and re-post path.
/// </summary>
public class PostingSuspenseItem : AuditableAggregateRoot
{
    protected PostingSuspenseItem() { }

    public PostingSuspenseItem(
        Guid companyId,
        string sourceModule,
        string sourceReference,
        Guid? accountId,
        decimal debit,
        decimal credit,
        Guid? currencyId,
        string reasonCode,
        string errorMessage)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        SourceModule = sourceModule;
        SourceReference = sourceReference;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        CurrencyId = currencyId;
        ReasonCode = reasonCode;
        ErrorMessage = errorMessage;
        Status = SuspenseStatus.Pending;
    }

    public Guid CompanyId { get; private set; }
    public string SourceModule { get; private set; } = string.Empty;
    public string SourceReference { get; private set; } = string.Empty;
    public Guid? AccountId { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public Guid? CurrencyId { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public SuspenseStatus Status { get; private set; }
    public Guid? ResolvedBatchId { get; private set; }

    public void Resolve(Guid resolvedBatchId)
    {
        Status = SuspenseStatus.Resolved;
        ResolvedBatchId = resolvedBatchId;
    }

    public void Discard(string? note = null)
    {
        Status = SuspenseStatus.Discarded;
        ErrorMessage = note ?? ErrorMessage;
    }
}

public enum SuspenseStatus
{
    Pending = 0,
    Resolved = 1,
    Discarded = 2
}

/// <summary>
/// A mid-year transfer of budget amount from one period to another (per account) with approval.
/// </summary>
public class BudgetTransfer : AuditableAggregateRoot
{
    protected BudgetTransfer() { }

    public BudgetTransfer(
        Guid companyId,
        Guid budgetId,
        Guid accountId,
        int fromPeriodNumber,
        int toPeriodNumber,
        decimal amount,
        string reason)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BudgetId = budgetId;
        AccountId = accountId;
        FromPeriodNumber = fromPeriodNumber;
        ToPeriodNumber = toPeriodNumber;
        Amount = amount;
        Reason = reason;
    }

    public Guid CompanyId { get; private set; }
    public Guid BudgetId { get; private set; }
    public Guid AccountId { get; private set; }
    public int FromPeriodNumber { get; private set; }
    public int ToPeriodNumber { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
}

/// <summary>
/// Realized/unrealized gain-loss produced by the multi-currency revaluation engine, per account.
/// </summary>
public class GlGainLoss : AuditableAggregateRoot
{
    protected GlGainLoss() { }

    public GlGainLoss(
        Guid companyId,
        Guid fiscalPeriodId,
        Guid? batchId,
        Guid accountId,
        Guid? currencyId,
        decimal gainLossAmount,
        DateTimeOffset revaluationDate)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        FiscalPeriodId = fiscalPeriodId;
        BatchId = batchId;
        AccountId = accountId;
        CurrencyId = currencyId;
        GainLossAmount = gainLossAmount;
        RevaluationDate = revaluationDate;
    }

    public Guid CompanyId { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public Guid? BatchId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CurrencyId { get; private set; }
    public decimal GainLossAmount { get; private set; }
    public DateTimeOffset RevaluationDate { get; private set; }
}
