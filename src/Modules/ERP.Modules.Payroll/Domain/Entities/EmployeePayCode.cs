// <copyright file="EmployeePayCode.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>Employee-specific pay code enrollment (e.g., override rate, billable flag).</summary>
public class EmployeePayCode : AuditableEntity
{
    protected EmployeePayCode() { }

    public EmployeePayCode(Guid employeeId, Guid payCodeId, decimal? overrideRate = null, bool isBillable = true)
        : base(Guid.NewGuid())
    {
        EmployeeId = employeeId;
        PayCodeId = payCodeId;
        OverrideRate = overrideRate;
        IsBillable = isBillable;
    }

    public Guid EmployeeId { get; private set; }
    public Guid PayCodeId { get; private set; }
    public decimal? OverrideRate { get; private set; }
    public bool IsBillable { get; private set; }
}
