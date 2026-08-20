// <copyright file="Project.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class Project : AuditableEntity
{
    private readonly List<ProjectTask> _tasks = [];
    private readonly List<BudgetLine> _budgetLines = [];
    private readonly List<CostTransaction> _costTransactions = [];
    private readonly List<ChangeOrder> _changeOrders = [];
    private readonly List<ContractLine> _contractLines = [];
    private readonly List<BillingSchedule> _billingSchedules = [];
    private readonly List<ProjectAllocationRule> _allocationRules = [];
    private readonly List<Subcontract> _subcontracts = [];

    protected Project() { }

    public Project(
        Guid companyId,
        string projectCode,
        string name,
        ProjectType projectType,
        Guid? customerId,
        string? projectManager,
        string? description = null,
        decimal? contractValue = null,
        DateTime? plannedStartDate = null,
        DateTime? plannedEndDate = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(projectCode))
            throw new ArgumentException("Project code is required.", nameof(projectCode));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));

        CompanyId = companyId;
        ProjectCode = projectCode;
        Name = name;
        ProjectType = projectType;
        CustomerId = customerId;
        ProjectManager = projectManager;
        Description = description;
        ContractValue = contractValue;
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
        Status = ProjectStatus.Planning;
        OriginalBudget = 0;
        RevisedBudget = 0;
        CostsToDate = 0;
        RevenueToDate = 0;
        PercentComplete = 0;
        RetainagePercentage = 0;
        RetainageHeld = 0;
    }

    public Guid CompanyId { get; private set; }
    public string ProjectCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectType ProjectType { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? ProjectManager { get; private set; }
    public decimal? ContractValue { get; private set; }
    public decimal OriginalBudget { get; private set; }
    public decimal RevisedBudget { get; private set; }
    public decimal CostsToDate { get; private set; }
    public decimal RevenueToDate { get; private set; }
    public decimal PercentComplete { get; private set; }
    public decimal RetainagePercentage { get; private set; }
    public decimal RetainageHeld { get; private set; }
    public decimal? ProfitMargin => ContractValue > 0 ? (ContractValue - RevisedBudget) / ContractValue * 100 : null;
    public DateTime? PlannedStartDate { get; private set; }
    public DateTime? PlannedEndDate { get; private set; }
    public DateTime? ActualStartDate { get; private set; }
    public DateTime? ActualEndDate { get; private set; }
    public bool IsBilled { get; set; }
    public bool IsClosed { get; private set; }

    /// <summary>Gets the separate contingency / management reserve amount on the budget.</summary>
    public decimal ContingencyAmount { get; private set; }

    /// <summary>Gets the amount of contingency reserve already released via change order.</summary>
    public decimal ReleasedContingency { get; private set; }

    /// <summary>Gets a value indicating whether invoicing is held (dispute, customer request, compliance).</summary>
    public bool BillingHold { get; private set; }

    /// <summary>Gets the reason for the billing hold.</summary>
    public string? BillingHoldReason { get; private set; }

    /// <summary>Gets the contract currency code for multi-currency projects (spec §5.6).</summary>
    public string? CurrencyCode { get; private set; }

    /// <summary>Gets the exchange rate applied to the contract currency.</summary>
    public decimal ExchangeRate { get; private set; } = 1m;

    /// <summary>Gets the remaining contingency available for release.</summary>
    public decimal RemainingContingency => ContingencyAmount - ReleasedContingency;

    /// <summary>Gets the estimate at completion (EAC) used as the denominator for the cost-to-cost % complete measurement basis.</summary>
    public decimal EstimateAtCompletion { get; private set; }

    /// <summary>Gets the accounting method elected for this project (percentage-of-completion vs. completed-contract).</summary>
    public AccountingMethod AccountingMethod { get; private set; } = AccountingMethod.PercentageOfCompletion;

    /// <summary>Gets the cumulative expected-loss accrual recognized on this project (GAAP: recognized immediately when EAC &lt; contract value).</summary>
    public decimal AccruedLoss { get; private set; }

    /// <summary>Gets a value indicating whether an expected-loss accrual has been recorded for this project.</summary>
    public bool LossAccrued => AccruedLoss != 0;

    /// <summary>Gets the user who recorded the expected-loss accrual.</summary>
    public Guid? LossAccruedBy { get; private set; }

    /// <summary>Gets the date the expected-loss accrual was recorded.</summary>
    public DateTime? LossAccruedOn { get; private set; }

    /// <summary>Gets the user who approved billing for this project (review/approval gate), if any.</summary>
    public Guid? BillingApprovedBy { get; private set; }

    /// <summary>Gets the date billing was approved.</summary>
    public DateTime? BillingApprovedOn { get; private set; }

    /// <summary>Gets a value indicating whether the project has been closed out (final billing, retention released, archived).</summary>
    public bool IsCloseOutComplete { get; private set; }

    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyCollection<BudgetLine> BudgetLines => _budgetLines.AsReadOnly();
    public IReadOnlyCollection<CostTransaction> CostTransactions => _costTransactions.AsReadOnly();
    public IReadOnlyCollection<ChangeOrder> ChangeOrders => _changeOrders.AsReadOnly();
    public IReadOnlyCollection<ContractLine> ContractLines => _contractLines.AsReadOnly();
    public IReadOnlyCollection<BillingSchedule> BillingSchedules => _billingSchedules.AsReadOnly();
    public IReadOnlyCollection<ProjectAllocationRule> AllocationRules => _allocationRules.AsReadOnly();
    public IReadOnlyCollection<Subcontract> Subcontracts => _subcontracts.AsReadOnly();

    public void Update(
        string? name,
        string? description,
        ProjectType? projectType,
        Guid? customerId,
        string? projectManager,
        decimal? contractValue,
        decimal? retainagePercentage,
        DateTime? plannedStartDate,
        DateTime? plannedEndDate)
    {
        if (name is not null)
        {
            Name = name;
        }

        if (description is not null)
        {
            Description = description;
        }

        if (projectType.HasValue)
        {
            ProjectType = projectType.Value;
        }

        CustomerId = customerId;
        ProjectManager = projectManager;

        if (contractValue.HasValue)
        {
            ContractValue = contractValue;
        }

        if (retainagePercentage.HasValue)
        {
            RetainagePercentage = retainagePercentage.Value;
        }

        if (plannedStartDate.HasValue)
        {
            PlannedStartDate = plannedStartDate;
        }

        if (plannedEndDate.HasValue)
        {
            PlannedEndDate = plannedEndDate;
        }
    }

    public void UpdateStatus(ProjectStatus status)
    {
        Status = status;
        if (status == ProjectStatus.Active && ActualStartDate is null)
            ActualStartDate = DateTime.UtcNow;
        if (status == ProjectStatus.Completed)
            ActualEndDate = DateTime.UtcNow;
        if (status == ProjectStatus.Closed)
            IsClosed = true;
    }

    public void AddRetainageHeld(decimal amount)
    {
        RetainageHeld += amount;
    }

    public void AddRevenue(decimal amount)
    {
        RevenueToDate += amount;
    }

    public void AdjustContractValue(decimal adjustment)
    {
        ContractValue += adjustment;
    }

    public void SetContingency(decimal contingencyAmount)
    {
        ContingencyAmount = contingencyAmount;
        RecalculateBudget();
    }

    /// <summary>Releases part of the contingency reserve via an approved change order that lifts the revised budget.</summary>
    /// <param name="amount">The amount of contingency to release.</param>
    /// <param name="reason">Optional reason for the release.</param>
    /// <returns>The created and approved change order.</returns>
    public ChangeOrder ReleaseContingency(decimal amount, string? reason = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (amount > ContingencyAmount - ReleasedContingency)
            throw new InvalidOperationException("Release exceeds remaining contingency reserve.");

        ReleasedContingency += amount;
        var co = AddChangeOrder(reason ?? "Contingency release", amount, CostCategory.Other, "Management reserve release");
        co.UpdateStatus(ChangeOrderStatus.Approved, "system");
        RecalculateBudget();
        return co;
    }

    public void SetBillingHold(bool hold, string? reason = null)
    {
        BillingHold = hold;
        BillingHoldReason = hold ? reason : null;
    }

    public void SetCurrency(string? currencyCode, decimal exchangeRate = 1m)
    {
        CurrencyCode = currencyCode;
        ExchangeRate = exchangeRate > 0 ? exchangeRate : 1m;
    }

    public void RecalculateBudget()
    {
        OriginalBudget = _budgetLines.Where(b => !b.IsRevised).Sum(b => b.BudgetAmount);
        RevisedBudget = _budgetLines.Sum(b => b.BudgetAmount) + ContingencyAmount;
    }

    public void RecalculateCosts()
    {
        CostsToDate = _costTransactions.Where(t => t.Status == TransactionStatus.Posted).Sum(t => t.Amount);
    }

    public void UpdatePercentComplete(decimal? costToCostPercent = null, decimal? physicalPercent = null)
    {
        if (physicalPercent.HasValue)
        {
            PercentComplete = physicalPercent.Value;
        }
        else if (costToCostPercent.HasValue && RevisedBudget > 0)
        {
            PercentComplete = costToCostPercent.Value;
        }
    }

    public void SetRetainage(decimal retainagePercentage)
    {
        RetainagePercentage = retainagePercentage;
    }

    /// <summary>Sets the estimate at completion (EAC) — the revised total cost forecast used as the
    /// denominator of the cost-to-cost percent-complete measurement basis.</summary>
    /// <param name="eac">The estimate at completion amount.</param>
    public void SetEstimateAtCompletion(decimal eac)
    {
        if (eac < 0)
            throw new ArgumentException("EAC cannot be negative.", nameof(eac));
        EstimateAtCompletion = eac;
    }

    /// <summary>Elected accounting method for this contract (percentage-of-completion vs. completed-contract).</summary>
    /// <param name="method">The accounting method.</param>
    public void SetAccountingMethod(AccountingMethod method)
    {
        AccountingMethod = method;
    }

    /// <summary>Recognizes an expected-loss accrual immediately per GAAP when the estimate at completion
    /// exceeds the contract value (loss-making contract). The accrual equals contract value − EAC.</summary>
    /// <param name="accruedBy">The identifier of the user recording the accrual.</param>
    public void AccrueLoss(Guid accruedBy)
    {
        var contractValue = ContractValue ?? 0;
        var eac = EstimateAtCompletion > 0 ? EstimateAtCompletion : RevisedBudget;
        if (eac <= contractValue)
            throw new InvalidOperationException("No expected loss: EAC does not exceed contract value.");
        AccruedLoss = eac - contractValue;
        LossAccruedBy = accruedBy;
        LossAccruedOn = DateTime.UtcNow;
    }

    /// <summary>Computes the revenue to recognize for the period under the percentage-of-completion method:
    /// earned revenue = cost-to-cost % complete × contract value, less revenue recognized to date.</summary>
    /// <returns>The revenue amount to recognize this period.</returns>
    public decimal ComputeRevenueToRecognize()
    {
        if (AccountingMethod == AccountingMethod.CompletedContract)
            return 0; // No revenue recognized until project completion.
        var contractValue = ContractValue ?? 0;
        if (contractValue == 0)
            return 0;
        var eac = EstimateAtCompletion > 0 ? EstimateAtCompletion : RevisedBudget;
        var percentComplete = eac > 0 ? CostsToDate / eac : 0;
        var earnedRevenue = percentComplete * contractValue;
        return earnedRevenue - RevenueToDate;
    }

    /// <summary>Approves billing for the project (review/approval gate before invoice generation).</summary>
    /// <param name="approvedBy">The identifier of the user approving billing.</param>
    public void ApproveBilling(Guid approvedBy)
    {
        BillingApprovedBy = approvedBy;
        BillingApprovedOn = DateTime.UtcNow;
    }

    /// <summary>Releases held retainage back to billable (e.g., final approval / % complete trigger).</summary>
    /// <param name="amount">The amount of retainage to release.</param>
    public void ReleaseRetainage(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (amount > RetainageHeld)
            throw new InvalidOperationException("Release exceeds retainage held.");
        RetainageHeld -= amount;
    }

    /// <summary>Marks the project closed out (final billing, retention released, archived).</summary>
    public void CompleteCloseOut()
    {
        IsCloseOutComplete = true;
        if (Status != ProjectStatus.Closed)
            UpdateStatus(ProjectStatus.Closed);
    }

    public ProjectTask AddTask(
        string taskCode,
        string description,
        Guid? parentTaskId,
        decimal? budgetedHours,
        decimal? budgetedCost)
    {
        var task = new ProjectTask(Id, taskCode, description, parentTaskId, budgetedHours, budgetedCost);
        _tasks.Add(task);
        return task;
    }

    public void RemoveTask(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is not null)
            _tasks.Remove(task);
    }

    public ChangeOrder AddChangeOrder(
        string description,
        decimal amount,
        CostCategory category,
        string? reason = null)
    {
        var co = new ChangeOrder(Id, description, amount, category, reason);
        _changeOrders.Add(co);
        return co;
    }

    public BudgetLine AddBudgetLine(
        Guid taskId,
        CostCategory category,
        decimal budgetAmount,
        decimal? budgetedHours,
        string? description = null)
    {
        var line = new BudgetLine(Id, taskId, category, budgetAmount, budgetedHours, description);
        _budgetLines.Add(line);
        return line;
    }
}
