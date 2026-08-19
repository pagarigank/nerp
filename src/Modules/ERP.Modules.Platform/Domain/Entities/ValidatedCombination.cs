// <copyright file="ValidatedCombination.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

public class ValidatedCombination : AuditableAggregateRoot
{
    protected ValidatedCombination() { }

    public ValidatedCombination(
        Guid companyId,
        string combinationKey,
        string segmentValuesJson,
        string? description = null) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        CombinationKey = combinationKey ?? throw new ArgumentNullException(nameof(combinationKey));
        SegmentValuesJson = segmentValuesJson ?? throw new ArgumentNullException(nameof(segmentValuesJson));
        Description = description;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public string CombinationKey { get; private set; } = string.Empty;

    public string SegmentValuesJson { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public string? Description { get; private set; }

    public void Update(string combinationKey, string segmentValuesJson, string? description)
    {
        CombinationKey = combinationKey ?? throw new ArgumentNullException(nameof(combinationKey));
        SegmentValuesJson = segmentValuesJson ?? throw new ArgumentNullException(nameof(segmentValuesJson));
        Description = description;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
