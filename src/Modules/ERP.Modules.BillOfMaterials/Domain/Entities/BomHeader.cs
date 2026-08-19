// <copyright file="BomHeader.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class BomHeader : AuditableEntity
{
    private readonly List<BomComponentLine> _components = [];
    private readonly List<BomRevisionHistory> _revisions = [];
    private readonly List<BomComponentSubstitution> _substitutions = [];
    private readonly List<ComponentAllocation> _allocations = [];
    private readonly List<EngineeringChangeNotice> _ecns = [];

    protected BomHeader() { }

    public BomHeader(
        Guid companyId,
        Guid parentItemId,
        string revision,
        BomType bomType,
        BomStatus status,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        string? description = null,
        decimal? yieldPercentage = null,
        string? alternateCode = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(revision))
            throw new ArgumentException("Revision is required.", nameof(revision));

        CompanyId = companyId;
        ParentItemId = parentItemId;
        Revision = revision;
        BomType = bomType;
        Status = status;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Description = description;
        YieldPercentage = yieldPercentage ?? 100m;
        AlternateCode = alternateCode;
    }

    public Guid CompanyId { get; private set; }
    public Guid ParentItemId { get; private set; }
    public string Revision { get; private set; } = string.Empty;
    public string? AlternateCode { get; private set; }
    public string? Description { get; private set; }
    public BomType BomType { get; private set; }
    public BomStatus Status { get; private set; }
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public decimal YieldPercentage { get; private set; }
    public decimal? EstimatedMaterialCost { get; private set; }
    public decimal? EstimatedLaborCost { get; private set; }
    public decimal? EstimatedOverheadCost { get; private set; }

    public IReadOnlyCollection<BomComponentLine> Components => _components.AsReadOnly();
    public IReadOnlyCollection<BomRevisionHistory> Revisions => _revisions.AsReadOnly();
    public IReadOnlyCollection<BomComponentSubstitution> Substitutions => _substitutions.AsReadOnly();
    public IReadOnlyCollection<ComponentAllocation> Allocations => _allocations.AsReadOnly();
    public IReadOnlyCollection<EngineeringChangeNotice> EngineeringChangeNotices => _ecns.AsReadOnly();

    public void Update(
        string? description,
        BomType? bomType,
        BomStatus? status,
        DateTime? effectiveFrom,
        DateTime? effectiveTo,
        decimal? yieldPercentage,
        string? alternateCode = null)
    {
        if (description is not null)
        {
            Description = description;
        }

        if (bomType.HasValue)
        {
            BomType = bomType.Value;
        }

        if (status.HasValue)
        {
            Status = status.Value;
        }

        if (effectiveFrom.HasValue)
        {
            EffectiveFrom = effectiveFrom;
        }

        if (effectiveTo.HasValue)
        {
            EffectiveTo = effectiveTo;
        }

        if (yieldPercentage.HasValue)
        {
            YieldPercentage = yieldPercentage.Value;
        }

        if (alternateCode is not null)
        {
            AlternateCode = alternateCode;
        }
    }

    public void UpdateEstimatedCosts(decimal materialCost, decimal laborCost, decimal overheadCost)
    {
        EstimatedMaterialCost = materialCost;
        EstimatedLaborCost = laborCost;
        EstimatedOverheadCost = overheadCost;
    }

    public BomComponentLine AddComponent(
        Guid componentItemId,
        decimal quantityPerParent,
        string unitOfMeasure,
        decimal? scrapFactor = null,
        int? operationSequence = null,
        Guid? workCenterId = null,
        bool isPhantom = false,
        bool isCritical = false,
        string? notes = null)
    {
        var line = new BomComponentLine(
            Id,
            componentItemId,
            quantityPerParent,
            unitOfMeasure,
            scrapFactor,
            operationSequence,
            workCenterId,
            isPhantom,
            isCritical,
            notes);
        _components.Add(line);
        return line;
    }

    public void RemoveComponent(Guid componentLineId)
    {
        var line = _components.FirstOrDefault(c => c.Id == componentLineId);
        if (line is not null)
            _components.Remove(line);
    }

    public BomComponentSubstitution AddSubstitution(
        Guid componentLineId, Guid substituteItemId, string reason, decimal? costVariance = null, int priority = 10)
    {
        var sub = new BomComponentSubstitution(Id, componentLineId, substituteItemId, reason, costVariance, priority);
        _substitutions.Add(sub);
        return sub;
    }

    public ComponentAllocation AddAllocation(
        Guid buildOrderId, Guid componentItemId, decimal quantity, string unitOfMeasure, Guid? warehouseId = null)
    {
        var alloc = new ComponentAllocation(Id, buildOrderId, componentItemId, quantity, unitOfMeasure, warehouseId);
        _allocations.Add(alloc);
        return alloc;
    }

    public EngineeringChangeNotice AddEngineeringChangeNotice(
        Guid companyId, string ecnNumber, string title, string description, DateTime? plannedEffectivity = null)
    {
        var ecn = new EngineeringChangeNotice(companyId, Id, ecnNumber, title, description, plannedEffectivity);
        _ecns.Add(ecn);
        return ecn;
    }
}
