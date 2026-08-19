// <copyright file="ArPhase4Entities.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsReceivable.Domain.Entities;

/// <summary>
/// Activity log on a customer account: collection notes, follow-ups, dunning actions,
/// promise-to-pay dates. Drives the collections workflow and collections dashboard.
/// </summary>
public class CollectionNote : AuditableAggregateRoot
{
    private readonly List<CollectionNoteActivity> _activities = [];

    protected CollectionNote() { }

    public CollectionNote(
        Guid companyId,
        Guid customerId,
        string note,
        string author,
        CollectionNoteType type,
        Guid? assignedTo = null,
        DateTimeOffset? followUpDate = null,
        string? relatedDocumentNumber = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Note is required.", nameof(note));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        CompanyId = companyId;
        CustomerId = customerId;
        Note = note;
        Author = author;
        Type = type;
        AssignedTo = assignedTo;
        FollowUpDate = followUpDate;
        RelatedDocumentNumber = relatedDocumentNumber;
        Status = CollectionNoteStatus.Open;
    }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Note { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public CollectionNoteType Type { get; private set; }

    public CollectionNoteStatus Status { get; private set; }

    public Guid? AssignedTo { get; private set; }

    public DateTimeOffset? FollowUpDate { get; private set; }

    public string? RelatedDocumentNumber { get; private set; }

    public DateTimeOffset? PromiseToPayDate { get; private set; }

    public IReadOnlyList<CollectionNoteActivity> Activities => _activities.AsReadOnly();

    public void AddActivity(string author, string description, CollectionNoteActivityType activityType)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        _activities.Add(new CollectionNoteActivity(Id, author, description, activityType));
    }

    public void SetPromiseToPay(DateTimeOffset promiseDate)
    {
        if (promiseDate <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Promise-to-pay date must be in the future.", nameof(promiseDate));
        PromiseToPayDate = promiseDate;
    }

    public void Assign(Guid? userId) => AssignedTo = userId;

    public void Close(string author)
    {
        if (Status == CollectionNoteStatus.Closed)
            throw new InvalidOperationException("Note is already closed.");
        Status = CollectionNoteStatus.Closed;
        AddActivity(author, "Note closed", CollectionNoteActivityType.StatusChange);
    }

    public void Reopen(string author)
    {
        if (Status == CollectionNoteStatus.Open)
            throw new InvalidOperationException("Note is already open.");
        Status = CollectionNoteStatus.Open;
        AddActivity(author, "Note reopened", CollectionNoteActivityType.StatusChange);
    }
}

public class CollectionNoteActivity : Entity
{
    protected CollectionNoteActivity() { }

    public CollectionNoteActivity(Guid collectionNoteId, string author, string description, CollectionNoteActivityType activityType)
        : base(Guid.NewGuid())
    {
        CollectionNoteId = collectionNoteId;
        Author = author;
        Description = description;
        ActivityType = activityType;
        ActivityDate = DateTimeOffset.UtcNow;
    }

    public Guid CollectionNoteId { get; private set; }

    public string Author { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public CollectionNoteActivityType ActivityType { get; private set; }

    public DateTimeOffset ActivityDate { get; private set; }
}

public enum CollectionNoteType
{
    Call = 0,
    Email = 1,
    Letter = 2,
    Meeting = 3,
    PromiseToPay = 4,
    Dispute = 5,
}

public enum CollectionNoteStatus
{
    Open = 0,
    Closed = 1,
}

public enum CollectionNoteActivityType
{
    Note = 0,
    StatusChange = 1,
    FollowUpScheduled = 2,
    PromiseToPay = 3,
    DisputeRaised = 4,
}

/// <summary>
/// Dunning letter template and escalation schedule. Each template targets an aging bucket
/// and a sequence (first / second / final notice). An auto-send job uses these to generate
/// dunning letters for customers in the matching bucket.
/// </summary>
public class DunningTemplate : AuditableAggregateRoot
{
    protected DunningTemplate() { }

    public DunningTemplate(
        Guid companyId,
        string name,
        string subject,
        string body,
        int sequence,
        DunningAgingBucket bucket,
        int minDaysOverdue,
        int maxDaysOverdue,
        bool sendEmail = true,
        bool sendPdf = false)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));
        if (sequence < 1)
            throw new ArgumentException("Sequence must be >= 1.", nameof(sequence));

        CompanyId = companyId;
        Name = name;
        Subject = subject;
        Body = body;
        Sequence = sequence;
        Bucket = bucket;
        MinDaysOverdue = minDaysOverdue;
        MaxDaysOverdue = maxDaysOverdue;
        SendEmail = sendEmail;
        SendPdf = sendPdf;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public int Sequence { get; private set; }

    public DunningAgingBucket Bucket { get; private set; }

    public int MinDaysOverdue { get; private set; }

    public int MaxDaysOverdue { get; private set; }

    public bool SendEmail { get; private set; }

    public bool SendPdf { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(
        string name,
        string subject,
        string body,
        int sequence,
        DunningAgingBucket bucket,
        int minDaysOverdue,
        int maxDaysOverdue,
        bool sendEmail,
        bool sendPdf,
        bool isActive)
    {
        Name = name;
        Subject = subject;
        Body = body;
        Sequence = sequence;
        Bucket = bucket;
        MinDaysOverdue = minDaysOverdue;
        MaxDaysOverdue = maxDaysOverdue;
        SendEmail = sendEmail;
        SendPdf = sendPdf;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

public enum DunningAgingBucket
{
    Current = 0,
    Days1To30 = 1,
    Days31To60 = 2,
    Days61To90 = 3,
    Over90 = 4,
}

/// <summary>
/// Periodic estimate of the allowance for doubtful accounts (bad-debt reserve) by aging bucket.
/// Each run posts an adjusting entry to the GL reserve account and tracks the resulting reserve balance.
/// </summary>
public class DoubtfulAccountAllowance : AuditableAggregateRoot
{
    private readonly List<AllowanceByBucket> _buckets = [];

    protected DoubtfulAccountAllowance() { }

    public DoubtfulAccountAllowance(
        Guid companyId,
        DateTimeOffset asOfDate,
        Guid reserveAccountId,
        string? notes = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        AsOfDate = asOfDate;
        ReserveAccountId = reserveAccountId;
        Notes = notes;
        Status = AllowanceRunStatus.Draft;
    }

    public Guid CompanyId { get; private set; }

    public DateTimeOffset AsOfDate { get; private set; }

    public Guid ReserveAccountId { get; private set; }

    public string Name { get; set; } = string.Empty;

    public AllowanceMethod Method { get; set; }

    public string? Notes { get; private set; }

    public AllowanceRunStatus Status { get; private set; }

    public decimal TotalEstimatedAllowance => _buckets.Sum(b => b.EstimatedAmount);

    public IReadOnlyList<AllowanceByBucket> Buckets => _buckets.AsReadOnly();

    public string? PostedBy { get; private set; }

    public DateTimeOffset? PostedOn { get; private set; }

    public void AddBucket(DunningAgingBucket bucket, decimal outstandingBalance, decimal reserveRate, decimal estimatedAmount)
    {
        _buckets.Add(new AllowanceByBucket(Id, bucket, outstandingBalance, reserveRate, estimatedAmount));
    }

    public void Post(string postedBy)
    {
        if (Status != AllowanceRunStatus.Draft)
            throw new InvalidOperationException("Only draft allowance runs can be posted.");
        Status = AllowanceRunStatus.Posted;
        PostedBy = postedBy;
        PostedOn = DateTimeOffset.UtcNow;
    }
}

public class AllowanceByBucket : Entity
{
    protected AllowanceByBucket() { }

    public AllowanceByBucket(Guid allowanceRunId, DunningAgingBucket bucket, decimal outstandingBalance, decimal reserveRate, decimal estimatedAmount)
        : base(Guid.NewGuid())
    {
        AllowanceRunId = allowanceRunId;
        Bucket = bucket;
        OutstandingBalance = outstandingBalance;
        ReserveRate = reserveRate;
        EstimatedAmount = estimatedAmount;
    }

    public Guid AllowanceRunId { get; private set; }

    public DunningAgingBucket Bucket { get; private set; }

    public decimal OutstandingBalance { get; private set; }

    public decimal ReserveRate { get; private set; }

    public decimal EstimatedAmount { get; private set; }
}

public enum AllowanceRunStatus
{
    Draft = 0,
    Posted = 1,
}

public enum AllowanceMethod
{
    PercentageOfReceivables = 0,
    AgingCategories = 1,
    Specific = 2,
}

/// <summary>
public class ResaleCertificate : AuditableAggregateRoot
{
    protected ResaleCertificate() { }

    public ResaleCertificate(
        Guid companyId,
        Guid customerId,
        string certificateNumber,
        string issuedState,
        DateTimeOffset issueDate,
        DateTimeOffset expiryDate,
        string? documentReference = null)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            throw new ArgumentException("Certificate number is required.", nameof(certificateNumber));
        if (string.IsNullOrWhiteSpace(issuedState))
            throw new ArgumentException("Issuing state is required.", nameof(issuedState));
        if (expiryDate <= issueDate)
            throw new ArgumentException("Expiry date must be after issue date.", nameof(expiryDate));

        CompanyId = companyId;
        CustomerId = customerId;
        CertificateNumber = certificateNumber;
        IssuedState = issuedState;
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        DocumentReference = documentReference;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string CertificateNumber { get; private set; } = string.Empty;

    public string IssuedState { get; private set; } = string.Empty;

    public DateTimeOffset IssueDate { get; private set; }

    public DateTimeOffset ExpiryDate { get; private set; }

    public string? DocumentReference { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow.Date > ExpiryDate.Date;

    public void Update(string certificateNumber, string issuedState, DateTimeOffset issueDate, DateTimeOffset expiryDate, string? documentReference, bool isActive)
    {
        CertificateNumber = certificateNumber;
        IssuedState = issuedState;
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        DocumentReference = documentReference;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
