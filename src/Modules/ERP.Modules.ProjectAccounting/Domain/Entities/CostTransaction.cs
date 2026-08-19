// <copyright file="CostTransaction.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class CostTransaction : AuditableEntity
{
    protected CostTransaction() { }

    public CostTransaction(
        Guid companyId,
        Guid projectId,
        Guid taskId,
        CostCategory category,
        CostTransactionType transactionType,
        decimal amount,
        decimal hours,
        string? description,
        Guid? sourceId,
        string? sourceReference,
        bool isBillable = true,
        Guid? vendorId = null,
        Guid? employeeId = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ProjectId = projectId;
        TaskId = taskId;
        Category = category;
        TransactionType = transactionType;
        Amount = amount;
        Hours = hours;
        Description = description;
        SourceId = sourceId;
        SourceReference = sourceReference;
        IsBillable = isBillable;
        VendorId = vendorId;
        EmployeeId = employeeId;
        Status = TransactionStatus.Posted;
        TransactionDate = DateTime.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public CostCategory Category { get; private set; }
    public CostTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Hours { get; private set; }
    public decimal BurdenAmount { get; private set; }
    public decimal BillableAmount { get; private set; }
    public string? Description { get; private set; }
    public Guid? SourceId { get; private set; }
    public string? SourceReference { get; private set; }
    public bool IsBillable { get; private set; }
    public Guid? VendorId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public bool IsAllocated { get; private set; }
    public bool IsBilled { get; private set; }

    public void SetBurden(decimal burdenAmount, decimal billableAmount)
    {
        BurdenAmount = burdenAmount;
        BillableAmount = billableAmount;
        IsAllocated = true;
    }

    public void MarkBilled()
    {
        IsBilled = true;
    }

    public void UpdateStatus(TransactionStatus status)
    {
        Status = status;
    }
}
