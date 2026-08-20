// <copyright file="SkillCertification.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.FieldService.Domain.Entities;

public class SkillCertification : AuditableEntity
{
    protected SkillCertification()
    {
    }

    public SkillCertification(
        Guid companyId,
        string code,
        string name,
        string? category,
        string? description)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Code = code;
        Name = name;
        Category = category;
        Description = description;
    }

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    public string? Description { get; private set; }
}

public class TechnicianSkill : AuditableEntity
{
    protected TechnicianSkill()
    {
    }

    public TechnicianSkill(
        Guid companyId,
        Guid technicianId,
        Guid skillCertificationId,
        int proficiency,
        DateTime? certifiedDate,
        DateTime? expirationDate)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        TechnicianId = technicianId;
        SkillCertificationId = skillCertificationId;
        Proficiency = proficiency;
        CertifiedDate = certifiedDate;
        ExpirationDate = expirationDate;
    }

    public Guid CompanyId { get; private set; }

    public Guid TechnicianId { get; private set; }

    public Guid SkillCertificationId { get; private set; }

    public int Proficiency { get; private set; }

    public DateTime? CertifiedDate { get; private set; }

    public DateTime? ExpirationDate { get; private set; }
}
