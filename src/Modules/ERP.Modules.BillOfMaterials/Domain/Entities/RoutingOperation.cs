// <copyright file="RoutingOperation.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class RoutingOperation : AuditableEntity
{
    protected RoutingOperation() { }

    public RoutingOperation(
        Guid companyId,
        string operationCode,
        string? description,
        Guid? workCenterId,
        decimal standardSetupTimeMinutes,
        decimal standardRunTimeMinutesPerUnit,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(operationCode))
            throw new ArgumentException("Operation code is required.", nameof(operationCode));

        CompanyId = companyId;
        OperationCode = operationCode;
        Description = description;
        WorkCenterId = workCenterId;
        StandardSetupTimeMinutes = standardSetupTimeMinutes;
        StandardRunTimeMinutesPerUnit = standardRunTimeMinutesPerUnit;
        IsActive = isActive;
    }

    public Guid CompanyId { get; private set; }
    public string OperationCode { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? WorkCenterId { get; private set; }
    public decimal StandardSetupTimeMinutes { get; private set; }
    public decimal StandardRunTimeMinutesPerUnit { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string? description,
        Guid? workCenterId,
        decimal? standardSetupTimeMinutes,
        decimal? standardRunTimeMinutesPerUnit)
    {
        if (description is not null)
        {
            Description = description;
        }

        WorkCenterId = workCenterId;

        if (standardSetupTimeMinutes.HasValue)
        {
            StandardSetupTimeMinutes = standardSetupTimeMinutes.Value;
        }

        if (standardRunTimeMinutesPerUnit.HasValue)
        {
            StandardRunTimeMinutesPerUnit = standardRunTimeMinutesPerUnit.Value;
        }
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
