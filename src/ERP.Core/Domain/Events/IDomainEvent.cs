namespace ERP.Core.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    Guid CorrelationId { get; }
    Guid CausationId { get; }
}