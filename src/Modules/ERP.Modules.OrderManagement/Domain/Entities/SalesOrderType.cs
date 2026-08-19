// <copyright file="SalesOrderType.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

public enum SalesOrderTypeCode
{
    Quote = 0,
    Order = 1,
    Return = 2,
    CreditOnly = 3
}

public class SalesOrderType : AuditableEntity
{
    private SalesOrderType() { }

    public SalesOrderType(Guid companyId, string code, string description, SalesOrderTypeCode typeCode, Guid? revenueAccountId)
    {
        CompanyId = companyId;
        Code = code;
        Description = description;
        TypeCode = typeCode;
        RevenueAccountId = revenueAccountId;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SalesOrderTypeCode TypeCode { get; private set; }
    public Guid? RevenueAccountId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string description, SalesOrderTypeCode typeCode, Guid? revenueAccountId, bool isActive)
    {
        Description = description;
        TypeCode = typeCode;
        RevenueAccountId = revenueAccountId;
        IsActive = isActive;
    }
}
