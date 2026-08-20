// <copyright file="PayCode.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Pay code master (regular, overtime, double-time, PTO, sick, holiday, bonus)
/// with GL account mapping for wage posting.
/// </summary>
public class PayCode : AuditableEntity
{
    protected PayCode() { }

    public PayCode(
        Guid companyId,
        string code,
        string description,
        PayCodeType type,
        string? glAccountNumber = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Pay code is required.", nameof(code));

        CompanyId = companyId;
        Code = code;
        Description = description;
        Type = type;
        GlAccountNumber = glAccountNumber;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PayCodeType Type { get; private set; }

    /// <summary>Gets the GL account number the wage for this pay code posts to (defaults to 6000 Salaries &amp; Wages).</summary>
    public string? GlAccountNumber { get; private set; }

    /// <summary>Gets a value indicating whether this pay code is overtime (time-and-a-half) eligible.</summary>
    public bool IsOvertime { get; private set; }

    /// <summary>Gets a value indicating whether this pay code counts as hours worked for prevailing-wage reporting.</summary>
    public bool CountsAsHoursWorked { get; private set; } = true;

    public void Update(string? description, string? glAccountNumber, bool? isOvertime, bool? countsAsHoursWorked)
    {
        if (description is not null) Description = description;
        if (glAccountNumber is not null) GlAccountNumber = glAccountNumber;
        if (isOvertime.HasValue) IsOvertime = isOvertime.Value;
        if (countsAsHoursWorked.HasValue) CountsAsHoursWorked = countsAsHoursWorked.Value;
    }
}

public enum PayCodeType
{
    Regular = 0,
    Overtime = 1,
    DoubleTime = 2,
    Pto = 3,
    Sick = 4,
    Holiday = 5,
    Bonus = 6,
    Other = 7,
}
