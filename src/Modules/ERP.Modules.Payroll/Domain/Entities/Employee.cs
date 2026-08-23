// <copyright file="Employee.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Employee master: identity, employment type, and encrypted-sensitive fields
/// (SSN, bank details) handled by the payroll setup per architecture.md §6 PII rules.
/// </summary>
public class Employee : AuditableEntity
{
    private readonly List<EmployeeCompensation> _compensationHistory = [];
    private readonly List<EmployeePayCode> _payCodes = [];

    protected Employee() { }

    public Employee(
        Guid companyId,
        string employeeCode,
        string firstName,
        string lastName,
        EmploymentType employmentType,
        DateTime hireDate)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            throw new ArgumentException("Employee code is required.", nameof(employeeCode));

        CompanyId = companyId;
        EmployeeCode = employeeCode;
        FirstName = firstName;
        LastName = lastName;
        EmploymentType = employmentType;
        HireDate = hireDate;
        Status = EmployeeStatus.Active;
    }

    public Guid CompanyId { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the encrypted SSN (column encrypted at rest; never exposed in audit logs).</summary>
    public string? SsnEncrypted { get; private set; }

    public EmploymentType EmploymentType { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public DateTime HireDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public string? Email { get; private set; }
    public Guid? DefaultProjectId { get; private set; }
    public string? DefaultRole { get; private set; }
    public decimal AllocationPercentage { get; private set; } = 100m;
    public bool IsBillable { get; private set; } = true;

    /// <summary>Mailing address used on year-end forms (W-2) — validated by the W-2 readiness job.</summary>
    public string? AddressLine1 { get; private set; }

    public string? City { get; private set; }

    public string? StateCode { get; private set; }

    public string? PostalCode { get; private set; }

    public IReadOnlyCollection<EmployeeCompensation> CompensationHistory => _compensationHistory.AsReadOnly();
    public IReadOnlyCollection<EmployeePayCode> PayCodes => _payCodes.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public void Update(
        string? firstName,
        string? lastName,
        string? email,
        Guid? defaultProjectId,
        string? defaultRole,
        decimal? allocationPercentage,
        bool? isBillable)
    {
        if (firstName is not null) FirstName = firstName;
        if (lastName is not null) LastName = lastName;
        if (email is not null) Email = email;
        if (defaultProjectId.HasValue) DefaultProjectId = defaultProjectId;
        if (defaultRole is not null) DefaultRole = defaultRole;
        if (allocationPercentage.HasValue)
        {
            if (allocationPercentage.Value < 0 || allocationPercentage.Value > 100)
                throw new ArgumentException("Allocation percentage must be between 0 and 100.", nameof(allocationPercentage));
            AllocationPercentage = allocationPercentage.Value;
        }

        if (isBillable.HasValue) IsBillable = isBillable.Value;
    }

    public void SetSsnEncrypted(string? ssnEncrypted)
    {
        SsnEncrypted = ssnEncrypted;
    }

    public void SetMailingAddress(string? addressLine1, string? city, string? stateCode, string? postalCode)
    {
        AddressLine1 = addressLine1;
        City = city;
        StateCode = stateCode;
        PostalCode = postalCode;
    }

    public void Terminate(DateTime terminationDate)
    {
        TerminationDate = terminationDate;
        Status = EmployeeStatus.Terminated;
    }

    public void Reactivate()
    {
        Status = EmployeeStatus.Active;
    }

    public EmployeeCompensation AddCompensation(
        decimal payRate,
        DateTime effectiveDate,
        decimal? overtimeRate = null,
        decimal? doubleTimeRate = null,
        bool isSalary = false,
        decimal? salaryAmount = null)
    {
        var comp = new EmployeeCompensation(Id, payRate, effectiveDate, overtimeRate, doubleTimeRate, isSalary, salaryAmount);
        _compensationHistory.Add(comp);
        return comp;
    }

    public EmployeePayCode AddPayCode(Guid payCodeId, decimal? overrideRate = null, bool isBillable = true)
    {
        var pc = new EmployeePayCode(Id, payCodeId, overrideRate, isBillable);
        _payCodes.Add(pc);
        return pc;
    }
}

public enum EmploymentType
{
    Hourly = 0,
    Salary = 1,
}

public enum EmployeeStatus
{
    Active = 0,
    Inactive = 1,
    Terminated = 2,
    Leave = 3,
}
