// <copyright file="FileArchivalPolicy.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class FileArchivalPolicy : AuditableEntity
{
    protected FileArchivalPolicy() { }

    public FileArchivalPolicy(
        Guid companyId,
        string integrationType,
        int retentionDays,
        string? archiveLocation)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        IntegrationType = integrationType;
        RetentionDays = retentionDays;
        ArchiveLocation = archiveLocation;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string IntegrationType { get; private set; } = string.Empty;
    public int RetentionDays { get; private set; }
    public string? ArchiveLocation { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(int retentionDays, string? archiveLocation)
    {
        RetentionDays = retentionDays;
        ArchiveLocation = archiveLocation;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
