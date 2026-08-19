using System.Threading.Tasks;

namespace ERP.Core.Domain.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(ERP.Core.Domain.Common.IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<ERP.Core.Domain.Common.IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}