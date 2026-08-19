// <copyright file="SalesTerritory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public class SalesTerritory : AuditableEntity
{
    private SalesTerritory() { }

    public SalesTerritory(Guid companyId, string code, string name, string? region, decimal defaultCommissionRate)
    {
        CompanyId = companyId;
        Code = code;
        Name = name;
        Region = region;
        DefaultCommissionRate = defaultCommissionRate;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public decimal DefaultCommissionRate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string? region, decimal defaultCommissionRate, bool isActive)
    {
        Name = name;
        Region = region;
        DefaultCommissionRate = defaultCommissionRate;
        IsActive = isActive;
    }
}
