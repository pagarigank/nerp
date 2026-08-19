// <copyright file="BomComponentSubstitution.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

/// <summary>
/// Defines an allowable substitute for a BOM component line, with cost variance tracking.
/// </summary>
public class BomComponentSubstitution : AuditableEntity
{
    protected BomComponentSubstitution() { }

    public BomComponentSubstitution(
        Guid bomHeaderId,
        Guid componentLineId,
        Guid substituteItemId,
        string reason,
        decimal? costVariance = null,
        int priority = 10)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Substitution reason is required.", nameof(reason));
        }

        BomHeaderId = bomHeaderId;
        ComponentLineId = componentLineId;
        SubstituteItemId = substituteItemId;
        Reason = reason;
        CostVariance = costVariance;
        Priority = priority;
    }

    public Guid BomHeaderId { get; private set; }
    public Guid ComponentLineId { get; private set; }
    public Guid SubstituteItemId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal? CostVariance { get; private set; }
    public int Priority { get; private set; }
    public bool IsApproved { get; private set; }

    public void Approve() => IsApproved = true;

    public void Update(string reason, decimal? costVariance, int priority)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = reason;
        }

        CostVariance = costVariance;
        Priority = priority;
    }
}
