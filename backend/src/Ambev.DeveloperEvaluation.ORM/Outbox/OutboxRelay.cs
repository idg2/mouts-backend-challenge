using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.ORM.Outbox;

// Work item: TASK-030 (FEAT-004), TASK-031 (FEAT-004)
/// <summary>
/// Runs one dispatch cycle of the outbox: publishes pending rows in <see cref="OutboxMessage.Sequence"/> order and
/// marks each one processed. The first failure ends the cycle, so no event overtakes an earlier one.
/// </summary>
/// <remarks>
/// Assumes one active relay: two relays over the same table could publish the same rows and reorder them.
/// </remarks>
public class OutboxRelay : IOutboxRelay
{
    private readonly DefaultContext _context;
    private readonly IEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxRelay> _logger;

    /// <summary>
    /// Initializes a new instance of OutboxRelay
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="publisher">The transport the events are handed to</param>
    /// <param name="timeProvider">The source of the processed time</param>
    /// <param name="logger">The logger</param>
    public OutboxRelay(DefaultContext context, IEventPublisher publisher, TimeProvider timeProvider, ILogger<OutboxRelay> logger)
    {
        _context = context;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Publishes up to <paramref name="batchSize"/> pending rows, oldest first.
    /// </summary>
    /// <param name="batchSize">The maximum number of rows read in this cycle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of rows published and marked processed</returns>
    public async Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pending = await _context.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.Sequence)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var dispatched = 0;
        foreach (var message in pending)
        {
            try
            {
                var type = IntegrationEventTypes.Find(message.Type)
                    ?? throw new InvalidOperationException($"Outbox event type '{message.Type}' is not registered");
                var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(message.Payload, type)!;
                await _publisher.PublishAsync(integrationEvent, message.Id, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception,
                    "Dispatch of outbox message {OutboxMessageId} ({OutboxMessageType}) failed; it and later messages wait for the next cycle",
                    message.Id, message.Type);
                break;
            }

            message.ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync(cancellationToken);
            dispatched++;
        }

        return dispatched;
    }
}
