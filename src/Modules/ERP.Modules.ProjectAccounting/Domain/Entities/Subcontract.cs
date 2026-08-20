// <copyright file="Subcontract.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Subcontract against a parent project/task: vendor, contract amount, retainage %,
/// scope, pay-when-paid flag, linked compliance (insurance/bond/certified payroll)
/// and lien waivers (spec §7.5 subcontract management).
/// </summary>
public class Subcontract : AuditableEntity
{
    private readonly List<SubcontractChangeOrder> _changeOrders = [];
    private readonly List<SubcontractInvoice> _invoices = [];
    private readonly List<SubcontractCompliance> _compliance = [];
    private readonly List<LienWaiver> _lienWaivers = [];

    protected Subcontract() { }

    public Subcontract(
        Guid companyId,
        Guid projectId,
        Guid? taskId,
        Guid vendorId,
        string subcontractNumber,
        decimal contractAmount,
        decimal retainagePercentage,
        string? scope = null,
        bool payWhenPaid = false)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(subcontractNumber))
            throw new ArgumentException("Subcontract number is required.", nameof(subcontractNumber));

        CompanyId = companyId;
        ProjectId = projectId;
        TaskId = taskId;
        VendorId = vendorId;
        SubcontractNumber = subcontractNumber;
        ContractAmount = contractAmount;
        RetainagePercentage = retainagePercentage;
        Scope = scope;
        PayWhenPaid = payWhenPaid;
        Status = SubcontractStatus.Active;
        BilledToDate = 0;
        RetainageHeld = 0;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid VendorId { get; private set; }
    public string SubcontractNumber { get; private set; } = string.Empty;
    public decimal ContractAmount { get; private set; }
    public decimal RetainagePercentage { get; private set; }
    public string? Scope { get; private set; }
    public bool PayWhenPaid { get; private set; }
    public SubcontractStatus Status { get; private set; }
    public decimal BilledToDate { get; private set; }
    public decimal RetainageHeld { get; private set; }
    public bool IsClosed { get; private set; }

    /// <summary>Gets the date this subcontract was executed, used for retainage-aging calculations.</summary>
    public DateTime SubcontractDate { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<SubcontractChangeOrder> ChangeOrders => _changeOrders.AsReadOnly();
    public IReadOnlyCollection<SubcontractInvoice> Invoices => _invoices.AsReadOnly();
    public IReadOnlyCollection<SubcontractCompliance> Compliance => _compliance.AsReadOnly();
    public IReadOnlyCollection<LienWaiver> LienWaivers => _lienWaivers.AsReadOnly();

    public void Update(decimal? contractAmount, decimal? retainagePercentage, string? scope, bool? payWhenPaid)
    {
        if (contractAmount.HasValue)
            ContractAmount = contractAmount.Value;
        if (retainagePercentage.HasValue)
            RetainagePercentage = retainagePercentage.Value;
        if (scope is not null)
            Scope = scope;
        if (payWhenPaid.HasValue)
            PayWhenPaid = payWhenPaid.Value;
    }

    public SubcontractChangeOrder AddChangeOrder(string description, decimal amount, string? reason = null)
    {
        var co = new SubcontractChangeOrder(Id, description, amount, reason);
        _changeOrders.Add(co);
        return co;
    }

    public SubcontractInvoice AddInvoice(
        string invoiceNumber, decimal amount, DateTime invoiceDate, decimal retainageRate, string? description = null)
    {
        var retained = amount * retainageRate / 100m;
        var inv = new SubcontractInvoice(Id, invoiceNumber, amount, invoiceDate, retainageRate, retained, description);
        _invoices.Add(inv);
        BilledToDate += amount;
        RetainageHeld += retained;
        return inv;
    }

    public SubcontractCompliance AddCompliance(string type, DateTime? expiryDate, string? documentReference = null)
    {
        var c = new SubcontractCompliance(Id, type, expiryDate, documentReference);
        _compliance.Add(c);
        return c;
    }

    public LienWaiver AddLienWaiver(string waiverType, decimal amount, DateTime effectiveDate, bool isFinal, string? description = null)
    {
        var w = new LienWaiver(Id, waiverType, amount, effectiveDate, isFinal, description);
        _lienWaivers.Add(w);
        return w;
    }

    public void ReleaseRetainage(decimal amount)
    {
        if (amount > RetainageHeld)
            throw new InvalidOperationException("Release exceeds held retainage.");
        RetainageHeld -= amount;
    }

    public void Close() => IsClosed = true;
}

public enum SubcontractStatus
{
    Active = 0,
    Completed = 1,
    Closed = 2,
}

public class SubcontractChangeOrder : AuditableEntity
{
    protected SubcontractChangeOrder() { }

    public SubcontractChangeOrder(Guid subcontractId, string description, decimal amount, string? reason)
        : base(Guid.NewGuid())
    {
        SubcontractId = subcontractId;
        Description = description;
        Amount = amount;
        Reason = reason;
        Status = SubcontractCoStatus.Draft;
    }

    public Guid SubcontractId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Reason { get; private set; }
    public SubcontractCoStatus Status { get; private set; }

    public void Approve() => Status = SubcontractCoStatus.Approved;
    public void Reject() => Status = SubcontractCoStatus.Rejected;
}

public enum SubcontractCoStatus
{
    Draft = 0,
    Approved = 1,
    Rejected = 2,
}

public class SubcontractInvoice : AuditableEntity
{
    protected SubcontractInvoice() { }

    public SubcontractInvoice(Guid subcontractId, string invoiceNumber, decimal amount, DateTime invoiceDate, decimal retainageRate, decimal retainageAmount, string? description)
        : base(Guid.NewGuid())
    {
        SubcontractId = subcontractId;
        InvoiceNumber = invoiceNumber;
        Amount = amount;
        InvoiceDate = invoiceDate;
        RetainageRate = retainageRate;
        RetainageAmount = retainageAmount;
        Description = description;
        IsPaid = false;
    }

    public Guid SubcontractId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public decimal RetainageRate { get; private set; }
    public decimal RetainageAmount { get; private set; }
    public string? Description { get; private set; }
    public bool IsPaid { get; private set; }

    public void MarkPaid() => IsPaid = true;
}

public class SubcontractCompliance : AuditableEntity
{
    protected SubcontractCompliance() { }

    public SubcontractCompliance(Guid subcontractId, string type, DateTime? expiryDate, string? documentReference)
        : base(Guid.NewGuid())
    {
        SubcontractId = subcontractId;
        Type = type;
        ExpiryDate = expiryDate;
        DocumentReference = documentReference;
    }

    public Guid SubcontractId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public DateTime? ExpiryDate { get; private set; }
    public string? DocumentReference { get; private set; }
    public bool IsCompliant => ExpiryDate is null || ExpiryDate >= DateTime.UtcNow.Date;
}

public class LienWaiver : AuditableEntity
{
    protected LienWaiver() { }

    public LienWaiver(Guid subcontractId, string waiverType, decimal amount, DateTime effectiveDate, bool isFinal, string? description)
        : base(Guid.NewGuid())
    {
        SubcontractId = subcontractId;
        WaiverType = waiverType;
        Amount = amount;
        EffectiveDate = effectiveDate;
        IsFinal = isFinal;
        Description = description;
    }

    public Guid SubcontractId { get; private set; }
    public string WaiverType { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public bool IsFinal { get; private set; }
    public string? Description { get; private set; }
}
