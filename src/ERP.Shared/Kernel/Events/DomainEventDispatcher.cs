// <copyright file="DomainEventDispatcher.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Shared.Kernel.Events;

/// <summary>
/// Dispatches domain events raised by <see cref="AggregateRoot"/> entities to
/// their registered <see cref="IDomainEventHandler{TEvent}"/> handlers. This is
/// the engine that makes the project's event-driven integration layer (3-way
/// match, AR/Inventory -> GL posting, committed-cost tracking, etc.) actually
/// execute. Previously this interface existed but was never implemented or
/// invoked, so every raised event was silently dropped.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public Task DispatchAsync(ERP.Core.Domain.Common.IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return DispatchCoreAsync(new[] { domainEvent }, cancellationToken);
    }

    public Task DispatchAsync(IEnumerable<ERP.Core.Domain.Common.IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = (domainEvents ?? Enumerable.Empty<ERP.Core.Domain.Common.IDomainEvent>()).ToList();
        if (events.Count == 0)
            return Task.CompletedTask;

        return DispatchCoreAsync(events, cancellationToken);
    }

    private async Task DispatchCoreAsync(IReadOnlyList<ERP.Core.Domain.Common.IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType).ToList();

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<ERP.Core.Domain.Common.IDomainEvent>.HandleAsync))
                    ?? throw new InvalidOperationException($"Handler {handlerType} is missing HandleAsync.");

                var task = (Task?)method.Invoke(handler, new object[] { domainEvent, cancellationToken });
                ArgumentNullException.ThrowIfNull(task);
                await task.ConfigureAwait(false);
            }
        }
    }
}