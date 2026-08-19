namespace ERP.Core.Domain.Events;

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public Guid CausationId { get; init; } = Guid.NewGuid();
}