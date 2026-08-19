// <copyright file="DispatchableDbContext.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Shared.Kernel.Events;

/// <summary>
/// Base class for module <c>DbContext</c>s that automatically dispatches domain
/// events raised by tracked <see cref="AggregateRoot"/> entities whenever the
/// unit of work is saved. The dispatcher is resolved from the EF Core internal
/// service provider, so no constructor change is required in derived contexts.
/// </summary>
public abstract class DispatchableDbContext : DbContext
{
    protected DispatchableDbContext(DbContextOptions options) : base(options)
    {
    }

    public override int SaveChanges()
    {
        var events = CollectDomainEvents();

        // Commit first, then dispatch. Dispatching inside the open source
        // transaction opens a second DbContext (e.g. the GL consumer) on a pooled
        // connection that can deadlock against the still-open transaction.
        var result = base.SaveChanges();

        if (events.Count > 0)
        {
            var dispatcher = this.GetService<ERP.Core.Domain.Events.IDomainEventDispatcher>();
            if (dispatcher is not null)
            {
                foreach (var domainEvent in events)
                    dispatcher.DispatchAsync(domainEvent).GetAwaiter().GetResult();
            }
        }

        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = CollectDomainEvents();

        // Commit first, then dispatch (see SaveChanges for the rationale).
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (events.Count > 0)
        {
            var dispatcher = this.GetService<ERP.Core.Domain.Events.IDomainEventDispatcher>();
            if (dispatcher is not null)
            {
                foreach (var domainEvent in events)
                    await dispatcher.DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private List<ERP.Core.Domain.Common.IDomainEvent> CollectDomainEvents()
    {
        var entries = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        var events = entries.SelectMany(e => e.Entity.DomainEvents).ToList();

        foreach (var entry in entries)
            entry.Entity.ClearDomainEvents();

        return events;
    }
}