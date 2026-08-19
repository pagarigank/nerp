// <copyright file="FOBTerm.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class FOBTerm : AuditableEntity
{
    protected FOBTerm() { }

    public FOBTerm(
        string code,
        string description,
        string freightResponsibility,
        string riskTransferPoint,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("FOB term code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Code = code;
        Description = description;
        FreightResponsibility = freightResponsibility;
        RiskTransferPoint = riskTransferPoint;
        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string FreightResponsibility { get; private set; } = string.Empty;

    public string RiskTransferPoint { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Description is required.", nameof(newDescription));

        Description = newDescription;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
