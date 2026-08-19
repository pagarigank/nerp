// <copyright file="ProjectStatus.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public enum ProjectStatus
{
    Planning = 0,
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Closed = 4,
}

public enum ProjectType
{
    TimeAndMaterials = 0,
    CostPlus = 1,
    FixedPrice = 2,
    UnitPrice = 3,
}

public enum CostCategory
{
    Labor = 0,
    Materials = 1,
    Subcontract = 2,
    Equipment = 3,
    Overhead = 4,
    Other = 5,
}

public enum ChangeOrderStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Executed = 4,
}

public enum BillingMethod
{
    Milestone = 0,
    PercentComplete = 1,
    Scheduled = 2,
    UnitPrice = 3,
    TimeAndMaterials = 4,
    CostPlus = 5,
}

public enum TransactionStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2,
}

public enum CostTransactionType
{
    ApVoucher = 0,
    PayrollLabor = 1,
    InventoryIssue = 2,
    SubcontractInvoice = 3,
    ManualAdjustment = 4,
    Burden = 5,
}
