// <copyright file="BomRevisionHistory.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public class BomRevisionHistory : AuditableEntity
{
    protected BomRevisionHistory() { }

    public BomRevisionHistory(
        Guid bomHeaderId,
        string revision,
        string changeDescription,
        string? reasonForChange = null,
        DateTime? effectiveDate = null)
        : base(Guid.NewGuid())
    {
        BomHeaderId = bomHeaderId;
        Revision = revision;
        ChangeDescription = changeDescription;
        ReasonForChange = reasonForChange;
        EffectiveDate = effectiveDate ?? DateTime.UtcNow;
    }

    public Guid BomHeaderId { get; private set; }
    public string Revision { get; private set; } = string.Empty;
    public string ChangeDescription { get; private set; } = string.Empty;
    public string? ReasonForChange { get; private set; }
    public DateTime? EffectiveDate { get; private set; }
}
