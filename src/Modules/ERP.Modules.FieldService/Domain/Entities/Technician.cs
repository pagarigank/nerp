// <copyright file="Technician.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public enum TechnicianStatus
{
    Active,
    Inactive,
    OnLeave
}

public class Technician : AuditableEntity
{
    protected Technician()
    {
    }

    public Technician(
        Guid companyId,
        Guid employeeId,
        string code,
        string firstName,
        string lastName,
        Guid? defaultTerritoryId,
        Guid? homeLocationId,
        TechnicianStatus status,
        string? email,
        string? phone,
        decimal hourlyRate)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        EmployeeId = employeeId;
        Code = code;
        FirstName = firstName;
        LastName = lastName;
        DefaultTerritoryId = defaultTerritoryId;
        HomeLocationId = homeLocationId;
        Status = status;
        Email = email;
        Phone = phone;
        HourlyRate = hourlyRate;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Guid? DefaultTerritoryId { get; private set; }
    public Guid? HomeLocationId { get; private set; }
    public TechnicianStatus Status { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public decimal HourlyRate { get; private set; }

    public void SetStatus(TechnicianStatus status) => Status = status;
}
