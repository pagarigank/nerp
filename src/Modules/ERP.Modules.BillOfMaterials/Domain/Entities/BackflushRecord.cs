// <copyright file="BackflushRecord.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

/// <summary>
/// Records a backflush (post-production auto-consumption) of standard component
/// quantities for a build order, with the variance against any previously issued
/// (manual) quantities (gap feature §677).
/// </summary>
public class BackflushRecord : AuditableEntity
{
    protected BackflushRecord() { }

    public BackflushRecord(
        Guid companyId,
        Guid buildOrderId,
        Guid bomHeaderId,
        decimal quantityBuilt)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        BuildOrderId = buildOrderId;
        BomHeaderId = bomHeaderId;
        QuantityBuilt = quantityBuilt;
        IsPosted = false;
    }

    public Guid CompanyId { get; private set; }
    public Guid BuildOrderId { get; private set; }
    public Guid BomHeaderId { get; private set; }
    public decimal QuantityBuilt { get; private set; }
    public decimal StandardComponentCost { get; private set; }
    public decimal ActualComponentCost { get; private set; }
    public decimal Variance => ActualComponentCost - StandardComponentCost;
    public bool IsPosted { get; private set; }

    public void SetCosts(decimal standardCost, decimal actualCost)
    {
        StandardComponentCost = standardCost;
        ActualComponentCost = actualCost;
    }

    public void MarkPosted() => IsPosted = true;
}
