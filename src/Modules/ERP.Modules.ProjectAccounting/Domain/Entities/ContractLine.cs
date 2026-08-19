// <copyright file="ContractLine.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class ContractLine : AuditableEntity
{
    protected ContractLine() { }

    public ContractLine(
        Guid projectId,
        string description,
        BillingMethod billingMethod,
        decimal contractAmount,
        decimal? unitPrice,
        decimal? unitQuantity,
        decimal? feePercentage,
        decimal? notToExceed,
        string? notes)
        : base(Guid.NewGuid())
    {
        ProjectId = projectId;
        Description = description;
        BillingMethod = billingMethod;
        ContractAmount = contractAmount;
        UnitPrice = unitPrice;
        UnitQuantity = unitQuantity;
        FeePercentage = feePercentage;
        NotToExceed = notToExceed;
        Notes = notes;
        BilledAmount = 0;
        PercentComplete = 0;
        IsActive = true;
    }

    public Guid ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public BillingMethod BillingMethod { get; private set; }
    public decimal ContractAmount { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public decimal? UnitQuantity { get; private set; }
    public decimal? FeePercentage { get; private set; }
    public decimal? NotToExceed { get; private set; }
    public string? Notes { get; private set; }
    public decimal BilledAmount { get; private set; }
    public decimal PercentComplete { get; private set; }
    public bool IsActive { get; private set; }
    public decimal Remaining => ContractAmount - BilledAmount;

    public void Update(
        string? description,
        decimal? contractAmount,
        decimal? unitPrice,
        decimal? feePercentage,
        decimal? notToExceed,
        bool? isActive)
    {
        if (description is not null)
        {
            Description = description;
        }

        if (contractAmount.HasValue)
        {
            ContractAmount = contractAmount.Value;
        }

        if (unitPrice.HasValue)
        {
            UnitPrice = unitPrice;
        }

        if (feePercentage.HasValue)
        {
            FeePercentage = feePercentage;
        }

        if (notToExceed.HasValue)
        {
            NotToExceed = notToExceed;
        }

        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
    }

    public void UpdateBilling(decimal billedAmount, decimal percentComplete)
    {
        BilledAmount = billedAmount;
        PercentComplete = percentComplete;
    }
}
