// <copyright file="SalesRep.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class SalesRep : AuditableEntity
{
    private SalesRep() { }

    public SalesRep(Guid companyId, string code, string name, decimal commissionRate, Guid? territoryId, string? email)
    {
        CompanyId = companyId;
        Code = code;
        Name = name;
        CommissionRate = commissionRate;
        TerritoryId = territoryId;
        Email = email;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal CommissionRate { get; private set; }
    public Guid? TerritoryId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Email { get; private set; }

    /// <summary>
    /// AP vendor used as the payable target when commission is accrued on a sale. Null until
    /// the rep is linked to an AP vendor (commission accrual is skipped when unlinked).
    /// </summary>
    public Guid? VendorId { get; private set; }

    /// <summary>Links this sales rep to an AP vendor so commission can be accrued as a payable.</summary>
    public void LinkVendor(Guid? vendorId) => VendorId = vendorId;

    public void Update(string name, decimal commissionRate, Guid? territoryId, bool isActive, string? email)
    {
        Name = name;
        CommissionRate = commissionRate;
        TerritoryId = territoryId;
        IsActive = isActive;
        Email = email;
    }
}
