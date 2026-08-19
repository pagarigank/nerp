using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Ardalis.Result;
using ERP.Core.Domain.Common;

namespace ERP.Shared.Kernel.Posting;

public enum PostingLineType
{
    Debit = 1,
    Credit = 2
}

public sealed record PostingLine
{
    [Required]
    [MaxLength(30)]
    public string Account { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resolved GL account identifier. When populated (in-process
    /// sub-ledger postings) it is used directly by the GL consumer, avoiding a
    /// string re-resolution round-trip.
    /// </summary>
    public Guid? AccountId { get; init; }

    [Required]
    public AccountKey Segments { get; init; } = null!;

    [Required]
    [Range(typeof(decimal), "0", "999999999999.9999")]
    public decimal Debit { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "999999999999.9999")]
    public decimal Credit { get; init; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; init; } = "USD";

    public PostingLineType LineType => Debit > 0 ? PostingLineType.Debit : PostingLineType.Credit;

    public decimal Amount => Debit > 0 ? Debit : Credit;

    public bool IsBalanced => Debit == Credit;

    public PostingLine WithDebit(decimal debit) => this with { Debit = debit, Credit = 0 };

    public PostingLine WithCredit(decimal credit) => this with { Credit = credit, Debit = 0 };

    public PostingLine WithAccountId(Guid accountId) => this with { AccountId = accountId };
}

public sealed record PostingMetadata
{
    [MaxLength(50)]
    public string? VendorId { get; init; }

    [MaxLength(50)]
    public string? CustomerId { get; init; }

    [MaxLength(50)]
    public string? ProjectId { get; init; }

    [MaxLength(50)]
    public string? SubcontractId { get; init; }

    [Required]
    [MaxLength(256)]
    public string PostedBy { get; init; } = string.Empty;

    [Required]
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public Guid? CausationId { get; init; }

    public Dictionary<string, string> CustomProperties { get; init; } = new();

    public static PostingMetadata Create(
        string postedBy,
        Guid correlationId,
        string? vendorId = null,
        string? customerId = null,
        string? projectId = null,
        string? subcontractId = null,
        Guid? causationId = null)
    {
        return new PostingMetadata
        {
            PostedBy = postedBy,
            CorrelationId = correlationId,
            VendorId = vendorId,
            CustomerId = customerId,
            ProjectId = projectId,
            SubcontractId = subcontractId,
            CausationId = causationId
        };
    }
}

public sealed record CanonicalPostingEvent
{
    public const string SchemaVersion = "1.0";

    [Required]
    [MaxLength(20)]
    public string SourceModule { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SourceDocumentId { get; init; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CompanyId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resolved company identifier (in-process postings).
    /// </summary>
    public Guid? CompanyGuid { get; init; }

    [Required]
    [MaxLength(7)]
    public string FiscalPeriod { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resolved fiscal period identifier (in-process postings).
    /// </summary>
    public Guid? FiscalPeriodGuid { get; init; }

    [Required]
    public DateTimeOffset PostingDate { get; init; } = DateTimeOffset.UtcNow;

    [Required]
    [MinLength(2)]
    public IReadOnlyList<PostingLine> Lines { get; init; } = [];

    [Required]
    public PostingMetadata Metadata { get; init; } = new();

    public bool IsBalanced => Lines.Sum(l => l.Debit) == Lines.Sum(l => l.Credit);

    public decimal TotalDebits => Lines.Sum(l => l.Debit);

    public decimal TotalCredits => Lines.Sum(l => l.Credit);

    public Result Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SourceModule))
            errors.Add("SourceModule is required");

        if (string.IsNullOrWhiteSpace(SourceDocumentId))
            errors.Add("SourceDocumentId is required");

        if (string.IsNullOrWhiteSpace(CompanyId) && CompanyGuid is null)
            errors.Add("CompanyId or CompanyGuid is required");

        if (string.IsNullOrWhiteSpace(FiscalPeriod) && FiscalPeriodGuid is null)
            errors.Add("FiscalPeriod or FiscalPeriodGuid is required");

        if (Lines.Count < 2)
            errors.Add("At least two posting lines are required");

        if (!IsBalanced)
            errors.Add($"Posting is not balanced: Debits={TotalDebits:N4}, Credits={TotalCredits:N4}");

        foreach (var line in Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Account) && line.AccountId is null)
                errors.Add("All lines must have an account (Account or AccountId)");

            if (line.Debit < 0 || line.Credit < 0)
                errors.Add("Debit and Credit amounts must be non-negative");

            if (line.Debit > 0 && line.Credit > 0)
                errors.Add("A line cannot have both debit and credit amounts");

            if (line.Debit == 0 && line.Credit == 0)
                errors.Add("A line must have either a debit or credit amount");

            if (string.IsNullOrWhiteSpace(line.Currency))
                errors.Add("Currency is required for each line");
        }

        if (string.IsNullOrWhiteSpace(Metadata.PostedBy))
            errors.Add("Metadata.PostedBy is required");

        return errors.Count > 0
            ? Result.Invalid(errors.Select(e => new ValidationError { ErrorMessage = e }).ToArray())
            : Result.Success();
    }

    /// <summary>
    /// Creates and validates a canonical posting event.
    /// </summary>
    /// <param name="sourceModule">The originating module code (e.g. "AP").</param>
    /// <param name="sourceDocumentId">The source document identifier.</param>
    /// <param name="companyId">The company code.</param>
    /// <param name="fiscalPeriod">The fiscal period code.</param>
    /// <param name="postingDate">The posting date.</param>
    /// <param name="lines">The posting lines.</param>
    /// <param name="metadata">The posting metadata.</param>
    /// <returns>The validated <see cref="CanonicalPostingEvent"/>.</returns>
    public static CanonicalPostingEvent Create(
        string sourceModule,
        string sourceDocumentId,
        string companyId,
        string fiscalPeriod,
        DateTimeOffset postingDate,
        IEnumerable<PostingLine> lines,
        PostingMetadata metadata)
    {
        var posting = new CanonicalPostingEvent
        {
            SourceModule = sourceModule,
            SourceDocumentId = sourceDocumentId,
            CompanyId = companyId,
            FiscalPeriod = fiscalPeriod,
            PostingDate = postingDate,
            Lines = lines.ToList().AsReadOnly(),
            Metadata = metadata
        };

        var validation = posting.Validate();
        if (!validation.IsSuccess)
            throw new InvalidOperationException($"Invalid posting event: {string.Join(", ", validation.Errors)}");

        return posting;
    }

    /// <summary>
    /// Convenience factory for in-process sub-ledger postings where the
    /// resolving identifiers (company / fiscal period GUIDs, GL account GUIDs)
    /// are already known.
    /// </summary>
    /// <param name="sourceModule">The originating module code (e.g. "AP").</param>
    /// <param name="sourceDocumentId">The source document identifier.</param>
    /// <param name="companyGuid">The resolved company identifier.</param>
    /// <param name="fiscalPeriodGuid">The resolved fiscal period identifier.</param>
    /// <param name="companyId">The company code.</param>
    /// <param name="fiscalPeriod">The fiscal period code.</param>
    /// <param name="postingDate">The posting date.</param>
    /// <param name="lines">The posting lines.</param>
    /// <param name="metadata">The posting metadata.</param>
    /// <returns>The validated <see cref="CanonicalPostingEvent"/>.</returns>
    public static CanonicalPostingEvent Create(
        string sourceModule,
        string sourceDocumentId,
        Guid companyGuid,
        Guid fiscalPeriodGuid,
        string companyId,
        string fiscalPeriod,
        DateTimeOffset postingDate,
        IEnumerable<PostingLine> lines,
        PostingMetadata metadata)
    {
        var posting = new CanonicalPostingEvent
        {
            SourceModule = sourceModule,
            SourceDocumentId = sourceDocumentId,
            CompanyGuid = companyGuid,
            FiscalPeriodGuid = fiscalPeriodGuid,
            CompanyId = companyId,
            FiscalPeriod = fiscalPeriod,
            PostingDate = postingDate,
            Lines = lines.ToList().AsReadOnly(),
            Metadata = metadata
        };

        var validation = posting.Validate();
        if (!validation.IsSuccess)
            throw new InvalidOperationException($"Invalid posting event: {string.Join(", ", validation.Errors)}");

        return posting;
    }
}

public interface IPostingEventPublisher
{
    /// <summary>
    /// Publishes a canonical posting event. Implementations forward the event to
    /// the GL consumer. Returns the GL <c>JournalBatch</c> identifier that was
    /// created, so the source sub-ledger can store a drill-back link.
    /// </summary>
    /// <param name="postingEvent">The canonical posting event to publish.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created GL <c>JournalBatch</c> identifier.</returns>
    Task<Guid> PublishAsync(CanonicalPostingEvent postingEvent, CancellationToken cancellationToken = default);
}

public interface IPostingEventConsumer
{
    /// <summary>
    /// Consumes a canonical posting event and materializes it in the General
    /// Ledger. Returns the created GL <c>JournalBatch</c> identifier.
    /// </summary>
    /// <param name="postingEvent">The canonical posting event to consume.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created GL <c>JournalBatch</c> identifier.</returns>
    Task<Guid> ConsumeAsync(CanonicalPostingEvent postingEvent, CancellationToken cancellationToken = default);
}