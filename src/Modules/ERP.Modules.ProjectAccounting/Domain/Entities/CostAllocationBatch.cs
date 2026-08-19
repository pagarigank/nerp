// <copyright file="CostAllocationBatch.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

/// <summary>
/// Batch of shared-cost allocations (IT, rent, utilities) distributed across
/// projects by an allocation base (e.g. direct labor $, headcount). Each line
/// posts a cost transaction to the target project (spec §7.3 cost allocation).
/// </summary>
public class CostAllocationBatch : AuditableEntity
{
    private readonly List<CostAllocationLine> _lines = [];

    protected CostAllocationBatch() { }

    public CostAllocationBatch(
        Guid companyId,
        string description,
        string allocationBase,
        DateTime periodStart,
        DateTime periodEnd)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        Description = description;
        AllocationBase = allocationBase;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Status = AllocationBatchStatus.Draft;
    }

    public Guid CompanyId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string AllocationBase { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public AllocationBatchStatus Status { get; private set; }
    public decimal TotalAllocated { get; private set; }

    public IReadOnlyCollection<CostAllocationLine> Lines => _lines.AsReadOnly();

    public CostAllocationLine AddLine(Guid projectId, decimal amount, CostCategory category, string? note = null)
    {
        var line = new CostAllocationLine(Id, projectId, amount, category, note);
        _lines.Add(line);
        return line;
    }

    public void Post(decimal total)
    {
        if (Status == AllocationBatchStatus.Posted)
            throw new InvalidOperationException("Allocation batch already posted.");
        TotalAllocated = total;
        Status = AllocationBatchStatus.Posted;
    }
}

public class CostAllocationLine : AuditableEntity
{
    protected CostAllocationLine() { }

    public CostAllocationLine(Guid batchId, Guid projectId, decimal amount, CostCategory category, string? note)
        : base(Guid.NewGuid())
    {
        BatchId = batchId;
        ProjectId = projectId;
        Amount = amount;
        Category = category;
        Note = note;
    }

    public Guid BatchId { get; private set; }
    public Guid ProjectId { get; private set; }
    public decimal Amount { get; private set; }
    public CostCategory Category { get; private set; }
    public string? Note { get; private set; }
    public bool IsPosted { get; private set; }

    public void MarkPosted() => IsPosted = true;
}

public enum AllocationBatchStatus
{
    Draft = 0,
    Posted = 1,
}
