// <copyright file="PurchaseOrderTemplate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class PurchaseOrderTemplate : AuditableEntity
{
    private readonly List<PurchaseOrderTemplateLine> _lines = [];

    protected PurchaseOrderTemplate() { }

    public PurchaseOrderTemplate(
        string templateCode,
        string templateName,
        Guid companyId,
        Guid vendorId,
        PurchaseOrderType orderType,
        string? description,
        decimal? blanketAmount,
        DateTime? effectiveDate,
        DateTime? expirationDate,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(templateCode))
            throw new ArgumentException("Template code is required.", nameof(templateCode));

        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required.", nameof(templateName));

        if (orderType == PurchaseOrderType.Blanket && !blanketAmount.HasValue)
            throw new ArgumentException("Blanket amount is required for blanket POs.", nameof(blanketAmount));

        TemplateCode = templateCode;
        TemplateName = templateName;
        CompanyId = companyId;
        VendorId = vendorId;
        OrderType = orderType;
        Description = description;
        BlanketAmount = blanketAmount;
        AmountUsed = 0;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        IsActive = isActive;
    }

    public string TemplateCode { get; private set; } = string.Empty;

    public string TemplateName { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public Guid VendorId { get; private set; }

    public PurchaseOrderType OrderType { get; private set; }

    public string? Description { get; private set; }

    public decimal? BlanketAmount { get; private set; }

    public decimal AmountUsed { get; private set; }

    public DateTime? EffectiveDate { get; private set; }

    public DateTime? ExpirationDate { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<PurchaseOrderTemplateLine> Lines => _lines.AsReadOnly();

    public void AddLine(PurchaseOrderTemplateLine line)
    {
        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line != null)
        {
            _lines.Remove(line);
        }
    }

    public void RecordRelease(decimal amount)
    {
        if (OrderType == PurchaseOrderType.Blanket && BlanketAmount.HasValue)
        {
            if (AmountUsed + amount > BlanketAmount.Value)
                throw new InvalidOperationException($"Release amount {amount} exceeds remaining blanket amount {BlanketAmount.Value - AmountUsed}.");

            AmountUsed += amount;
        }
    }

    public decimal GetRemainingAmount()
    {
        if (OrderType == PurchaseOrderType.Blanket && BlanketAmount.HasValue)
        {
            return BlanketAmount.Value - AmountUsed;
        }

        return 0;
    }

    public bool IsExpired()
    {
        return ExpirationDate.HasValue && DateTime.UtcNow > ExpirationDate.Value;
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
