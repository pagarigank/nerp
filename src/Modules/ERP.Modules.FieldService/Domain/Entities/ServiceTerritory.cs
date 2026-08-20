// <copyright file="ServiceTerritory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public class ServiceTerritory : AuditableEntity
{
    protected ServiceTerritory()
    {
    }

    public ServiceTerritory(
        Guid companyId,
        string code,
        string name,
        string? region,
        string? zipCoverage,
        Guid? defaultTechnicianId,
        decimal travelCostPerMile)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = code;
        Name = name;
        Region = region;
        ZipCoverage = zipCoverage;
        DefaultTechnicianId = defaultTechnicianId;
        TravelCostPerMile = travelCostPerMile;
    }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public string? ZipCoverage { get; private set; }
    public Guid? DefaultTechnicianId { get; private set; }
    public decimal TravelCostPerMile { get; private set; }
}
