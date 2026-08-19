// <copyright file="KitComponent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Inventory.Domain.Entities;

/// <summary>
/// A kit (bundled item) is composed of components. Receiving a kit receipt
/// increments the kit on-hand and decrements each component; issuing a kit
/// does the reverse. This is light kitting without full Phase 9 BOM structure.
/// </summary>
public class KitComponent : AuditableEntity
{
    private KitComponent() { }

    public KitComponent(
        Guid companyId,
        Guid kitItemId,
        Guid componentItemId,
        decimal quantityPerKit,
        string unitOfMeasure)
        : base(Guid.NewGuid())
    {
        if (quantityPerKit <= 0)
            throw new ArgumentException("Quantity per kit must be greater than zero.", nameof(quantityPerKit));

        CompanyId = companyId;
        KitItemId = kitItemId;
        ComponentItemId = componentItemId;
        QuantityPerKit = quantityPerKit;
        UnitOfMeasure = unitOfMeasure;
    }

    public Guid CompanyId { get; private set; }
    public Guid KitItemId { get; private set; }
    public Guid ComponentItemId { get; private set; }
    public decimal QuantityPerKit { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;

    public void Update(decimal quantityPerKit, string unitOfMeasure)
    {
        if (quantityPerKit <= 0)
            throw new ArgumentException("Quantity per kit must be greater than zero.", nameof(quantityPerKit));
        QuantityPerKit = quantityPerKit;
        UnitOfMeasure = unitOfMeasure;
    }

    // Navigation
    public Item? KitItem { get; set; }
    public Item? ComponentItem { get; set; }
}
