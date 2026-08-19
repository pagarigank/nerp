// <copyright file="EngineeringChangeNotice.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public enum EcnStatus
{
    Draft = 0,
    Submitted = 1,
    InReview = 2,
    Approved = 3,
    Rejected = 4,
    Executed = 5,
}

/// <summary>
/// Engineering Change Notice: proposed change to a BOM, reviewed and approved with
/// effectivity dates. Only an executed ECN changes the active BOM; audit trail per spec.
/// </summary>
public class EngineeringChangeNotice : AuditableEntity
{
    protected EngineeringChangeNotice() { }

    public EngineeringChangeNotice(
        Guid companyId,
        Guid bomHeaderId,
        string ecnNumber,
        string title,
        string description,
        DateTime? plannedEffectivity = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(ecnNumber))
        {
            throw new ArgumentException("ECN number required.", nameof(ecnNumber));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("ECN title required.", nameof(title));
        }

        CompanyId = companyId;
        BomHeaderId = bomHeaderId;
        EcnNumber = ecnNumber;
        Title = title;
        Description = description;
        PlannedEffectivity = plannedEffectivity;
        Status = EcnStatus.Draft;
    }

    public Guid CompanyId { get; private set; }
    public Guid BomHeaderId { get; private set; }
    public string EcnNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public EcnStatus Status { get; private set; }
    public DateTime? PlannedEffectivity { get; private set; }
    public DateTime? ActualEffectivity { get; private set; }
    public string? Reviewer { get; private set; }
    public string? Approver { get; private set; }
    public string? RejectionReason { get; private set; }

    public void Submit(string reviewer)
    {
        Status = EcnStatus.Submitted;
        Reviewer = reviewer;
    }

    public void StartReview()
    {
        Status = EcnStatus.InReview;
    }

    public void Approve(string approver)
    {
        Status = EcnStatus.Approved;
        Approver = approver;
    }

    public void Reject(string reason)
    {
        Status = EcnStatus.Rejected;
        RejectionReason = reason;
    }

    public void Execute(DateTime effectivity)
    {
        if (Status != EcnStatus.Approved)
        {
            throw new InvalidOperationException("ECN must be approved before execution.");
        }

        Status = EcnStatus.Executed;
        ActualEffectivity = effectivity;
    }

    public void Update(string title, string description, DateTime? plannedEffectivity)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        Description = description;
        PlannedEffectivity = plannedEffectivity;
    }
}
