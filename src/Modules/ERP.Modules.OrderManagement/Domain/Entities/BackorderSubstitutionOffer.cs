// <copyright file="BackorderSubstitutionOffer.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Substitute-item offer made to a customer when an ordered item is on backorder
/// (Phase 8 gap 584). The offer references an approved substitute (Phase 7
/// substitution) at an approved price; the customer accepts or rejects it, and on
/// acceptance the sales order line is redirected to the substitute item.
/// </summary>
public class BackorderSubstitutionOffer : AuditableEntity
{
    protected BackorderSubstitutionOffer() { }

    public BackorderSubstitutionOffer(
        Guid companyId,
        Guid salesOrderId,
        Guid salesOrderLineId,
        Guid originalItemId,
        Guid substituteItemId,
        decimal quantity,
        decimal approvedUnitPrice,
        string? reason = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        SalesOrderId = salesOrderId;
        SalesOrderLineId = salesOrderLineId;
        OriginalItemId = originalItemId;
        SubstituteItemId = substituteItemId;
        Quantity = quantity;
        ApprovedUnitPrice = approvedUnitPrice;
        Reason = reason;
        Status = SubstitutionOfferStatus.Pending;
    }

    public Guid CompanyId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public Guid SalesOrderLineId { get; private set; }
    public Guid OriginalItemId { get; private set; }
    public Guid SubstituteItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ApprovedUnitPrice { get; private set; }
    public string? Reason { get; private set; }
    public SubstitutionOfferStatus Status { get; private set; }
    public DateTime? RespondedDate { get; private set; }

    public void Accept()
    {
        if (Status != SubstitutionOfferStatus.Pending)
            throw new InvalidOperationException($"Cannot accept an offer in {Status} status.");
        Status = SubstitutionOfferStatus.Accepted;
        RespondedDate = DateTime.UtcNow;
    }

    public void Reject(string? reason = null)
    {
        if (Status != SubstitutionOfferStatus.Pending)
            throw new InvalidOperationException($"Cannot reject an offer in {Status} status.");
        Status = SubstitutionOfferStatus.Rejected;
        Reason = reason;
        RespondedDate = DateTime.UtcNow;
    }
}

public enum SubstitutionOfferStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}
