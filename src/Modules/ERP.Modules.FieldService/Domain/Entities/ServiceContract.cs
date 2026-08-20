// <copyright file="ServiceContract.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum ServiceContractStatus
{
    Draft,
    Active,
    Expired,
    Cancelled
}

public enum BillingType
{
    TimeAndMaterial,
    FixedPrice,
    PerVisit,
    Warranty
}

public class ServiceContract : AuditableEntity
{
    protected ServiceContract()
    {
    }

    public ServiceContract(
        Guid companyId,
        string contractNumber,
        string name,
        Guid customerId,
        DateTime startDate,
        DateTime endDate,
        BillingType billingType,
        decimal? contractValue,
        bool includesWarranty,
        int? warrantyMonths,
        string? notes)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        ContractNumber = contractNumber;
        Name = name;
        CustomerId = customerId;
        StartDate = startDate;
        EndDate = endDate;
        Status = ServiceContractStatus.Draft;
        BillingType = billingType;
        ContractValue = contractValue;
        IncludesWarranty = includesWarranty;
        WarrantyMonths = warrantyMonths;
        Notes = notes;
    }

    public Guid CompanyId { get; private set; }
    public string ContractNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ServiceContractStatus Status { get; private set; }
    public BillingType BillingType { get; private set; }
    public decimal? ContractValue { get; private set; }
    public bool IncludesWarranty { get; private set; }
    public int? WarrantyMonths { get; private set; }
    public string? Notes { get; private set; }

    public void Activate() => Status = ServiceContractStatus.Active;

    public void Expire() => Status = ServiceContractStatus.Expired;

    public void Cancel() => Status = ServiceContractStatus.Cancelled;
}
