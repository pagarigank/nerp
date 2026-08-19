// <copyright file="InProcessPostingEventPublisher.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using ERP.Shared.Kernel.Posting;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.GeneralLedger.Infrastructure.Posting;

/// <summary>
/// In-process implementation of the canonical posting pipeline. Sub-ledgers
/// (AP, AR, Cash, etc.) call <see cref="PublishAsync"/>; this forwards the
/// event to the registered <see cref="IPostingEventConsumer"/> (GL), which
/// materializes the balanced journal batch. Swapping in a message-broker
/// publisher later requires no changes at the call sites.
/// </summary>
public sealed class InProcessPostingEventPublisher : IPostingEventPublisher
{
    private readonly IPostingEventConsumer _consumer;
    private readonly ILogger<InProcessPostingEventPublisher> _logger;

    public InProcessPostingEventPublisher(
        IPostingEventConsumer consumer,
        ILogger<InProcessPostingEventPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _logger = logger;
    }

    public async Task<Guid> PublishAsync(CanonicalPostingEvent postingEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(postingEvent);

        var validation = postingEvent.Validate();
        if (!validation.IsSuccess)
        {
            throw new InvalidOperationException($"Refusing to post an invalid {postingEvent.SourceModule} event ({postingEvent.SourceDocumentId}): {string.Join("; ", validation.Errors)}");
        }

        _logger.LogInformation("Publishing {SourceModule} posting {SourceDocumentId} with {LineCount} lines.", postingEvent.SourceModule, postingEvent.SourceDocumentId, postingEvent.Lines.Count);

        return await _consumer.ConsumeAsync(postingEvent, cancellationToken);
    }
}
