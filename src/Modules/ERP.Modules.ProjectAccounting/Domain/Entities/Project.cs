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

    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();
    public IReadOnlyCollection<BudgetLine> BudgetLines => _budgetLines.AsReadOnly();
    public IReadOnlyCollection<CostTransaction> CostTransactions => _costTransactions.AsReadOnly();
    public IReadOnlyCollection<ChangeOrder> ChangeOrders => _changeOrders.AsReadOnly();
    public IReadOnlyCollection<ContractLine> ContractLines => _contractLines.AsReadOnly();
    public IReadOnlyCollection<BillingSchedule> BillingSchedules => _billingSchedules.AsReadOnly();
    public IReadOnlyCollection<ProjectAllocationRule> AllocationRules => _allocationRules.AsReadOnly();

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

    public void RecalculateBudget()
    {
        OriginalBudget = _budgetLines.Where(b => !b.IsRevised).Sum(b => b.BudgetAmount);
        RevisedBudget = _budgetLines.Sum(b => b.BudgetAmount);
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
