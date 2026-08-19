using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Core.Domain.Common;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity() { }

    protected AuditableEntity(Guid id) : base(id) { }

    [Required]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(256)]
    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedOn { get; set; }

    [MaxLength(256)]
    public string? DeletedBy { get; set; }

    public DateTimeOffset? DeletedOn { get; set; }

    public bool IsDeleted => DeletedOn.HasValue;

    public void MarkDeleted(string deletedBy)
    {
        DeletedBy = deletedBy;
        DeletedOn = DateTimeOffset.UtcNow;
    }

    public void MarkModified(string modifiedBy)
    {
        ModifiedBy = modifiedBy;
        ModifiedOn = DateTimeOffset.UtcNow;
    }
}

public abstract class AuditableAggregateRoot : AggregateRoot
{
    protected AuditableAggregateRoot() { }

    protected AuditableAggregateRoot(Guid id) : base(id) { }

    [Required]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(256)]
    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedOn { get; set; }

    [MaxLength(256)]
    public string? DeletedBy { get; set; }

    public DateTimeOffset? DeletedOn { get; set; }

    public bool IsDeleted => DeletedOn.HasValue;

    public void MarkDeleted(string deletedBy)
    {
        DeletedBy = deletedBy;
        DeletedOn = DateTimeOffset.UtcNow;
    }

    public void MarkModified(string modifiedBy)
    {
        ModifiedBy = modifiedBy;
        ModifiedOn = DateTimeOffset.UtcNow;
    }
}