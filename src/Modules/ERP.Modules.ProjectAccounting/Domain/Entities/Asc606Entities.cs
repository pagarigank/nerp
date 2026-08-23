// <copyright file="Asc606Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public enum ObligationStatus
{
    NotSatisfied = 0,
    PartiallySatisfied = 1,
    Satisfied = 2,
}

/// <summary>ASC 606 step 2 performance obligation with its step 3 allocated transaction price and step 5 recognition progress.</summary>
public class ContractPerformanceObligation : AuditableAggregateRoot
{
    private const decimal Epsilon = 0.005m;

    protected ContractPerformanceObligation() { }

    public ContractPerformanceObligation(
        Guid companyId,
        Guid projectId,
        string description,
        decimal transactionPriceAllocated,
        string? standaloneSellingPriceBasis)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (transactionPriceAllocated < 0)
            throw new ArgumentException("Allocated transaction price cannot be negative.", nameof(transactionPriceAllocated));

        CompanyId = companyId;
        ProjectId = projectId;
        Description = description;
        TransactionPriceAllocated = transactionPriceAllocated;
        StandaloneSellingPriceBasis = standaloneSellingPriceBasis;
        Status = ObligationStatus.NotSatisfied;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal TransactionPriceAllocated { get; private set; }
    public string? StandaloneSellingPriceBasis { get; private set; }
    public decimal RecognizedRevenueToDate { get; private set; }
    public ObligationStatus Status { get; private set; }
    public DateTimeOffset? SatisfiedOn { get; private set; }

    public bool IsUnrecognized => Status == ObligationStatus.NotSatisfied && RecognizedRevenueToDate == 0;

    public void RecordRecognition(decimal amount, DateTimeOffset asOf)
    {
        if (amount < 0)
            throw new ArgumentException("Recognition amount cannot be negative.", nameof(amount));
        if (RecognizedRevenueToDate + amount > TransactionPriceAllocated + Epsilon)
            throw new InvalidOperationException("Recognition exceeds the allocated transaction price.");

        RecognizedRevenueToDate += amount;
        if (RecognizedRevenueToDate >= TransactionPriceAllocated - Epsilon)
        {
            Status = ObligationStatus.Satisfied;
            SatisfiedOn = asOf;
        }
        else
        {
            Status = ObligationStatus.PartiallySatisfied;
            SatisfiedOn = null;
        }
    }

    public void Update(string? description, decimal? transactionPriceAllocated, string? standaloneSellingPriceBasis)
    {
        if (!IsUnrecognized)
            throw new InvalidOperationException("Only obligations without recorded recognition can be updated.");

        if (!string.IsNullOrWhiteSpace(description))
            Description = description;

        if (transactionPriceAllocated.HasValue)
        {
            if (transactionPriceAllocated.Value < 0)
                throw new ArgumentException("Allocated transaction price cannot be negative.", nameof(transactionPriceAllocated));
            TransactionPriceAllocated = transactionPriceAllocated.Value;
        }

        if (standaloneSellingPriceBasis is not null)
            StandaloneSellingPriceBasis = standaloneSellingPriceBasis;
    }

    public void SetAllocation(decimal transactionPriceAllocated)
    {
        if (!IsUnrecognized)
            throw new InvalidOperationException("Allocation cannot be changed after revenue has been recognized.");
        if (transactionPriceAllocated < 0)
            throw new ArgumentException("Allocated transaction price cannot be negative.", nameof(transactionPriceAllocated));

        TransactionPriceAllocated = transactionPriceAllocated;
        Status = ObligationStatus.NotSatisfied;
    }
}

/// <summary>Project attachment metadata; binary content lives in object storage referenced by <see cref="FileReference"/> (architecture §3).</summary>
public class ProjectDocument : AuditableAggregateRoot
{
    protected ProjectDocument() { }

    public ProjectDocument(
        Guid companyId,
        Guid projectId,
        string name,
        string documentType,
        string fileReference,
        string? contentType,
        long? sizeBytes,
        string uploadedBy)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("Document type is required.", nameof(documentType));
        if (string.IsNullOrWhiteSpace(fileReference))
            throw new ArgumentException("File reference is required.", nameof(fileReference));

        CompanyId = companyId;
        ProjectId = projectId;
        Name = name;
        DocumentType = documentType;
        FileReference = fileReference;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedBy = uploadedBy;
        UploadedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public string FileReference { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long? SizeBytes { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;
    public DateTimeOffset UploadedOn { get; private set; }
}

/// <summary>Daily point-in-time estimate-at-completion snapshot driving the profit-fade / EAC trend view.</summary>
public class ProjectEacSnapshot : AuditableEntity
{
    protected ProjectEacSnapshot() { }

    public ProjectEacSnapshot(
        Guid companyId,
        Guid projectId,
        DateTimeOffset capturedOn,
        decimal budgetAtCompletion,
        decimal estimateAtCompletion,
        decimal estimatedMarginPct,
        decimal? pendingChangeOrderAmount)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        ProjectId = projectId;
        CapturedOn = capturedOn;
        BudgetAtCompletion = budgetAtCompletion;
        EstimateAtCompletion = estimateAtCompletion;
        EstimatedMarginPct = estimatedMarginPct;
        PendingChangeOrderAmount = pendingChangeOrderAmount;
    }

    public Guid CompanyId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateTimeOffset CapturedOn { get; private set; }
    public decimal BudgetAtCompletion { get; private set; }
    public decimal EstimateAtCompletion { get; private set; }
    public decimal EstimatedMarginPct { get; private set; }
    public decimal? PendingChangeOrderAmount { get; private set; }

    public void UpdateValues(
        decimal budgetAtCompletion,
        decimal estimateAtCompletion,
        decimal estimatedMarginPct,
        decimal? pendingChangeOrderAmount)
    {
        BudgetAtCompletion = budgetAtCompletion;
        EstimateAtCompletion = estimateAtCompletion;
        EstimatedMarginPct = estimatedMarginPct;
        PendingChangeOrderAmount = pendingChangeOrderAmount;
    }
}
