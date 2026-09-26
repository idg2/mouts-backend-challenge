using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
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

    // Work item: TASK-054 (FEAT-017), TASK-076 (FEAT-003)
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
        StepTrace.Step("SAL-DSP-01", "Read up to BatchSize pending rows by Sequence",
            [("batchSize", batchSize), ("pending", pending.Count), ("firstSequence", pending.FirstOrDefault()?.Sequence)]);

        var dispatched = 0;
        foreach (var message in pending)
        {
            try
            {
                var type = IntegrationEventTypes.Find(message.Type);
                StepTrace.Step("SAL-DSP-02", "Type registered?",
                    [("rowId", message.Id), ("sequence", message.Sequence), ("eventType", message.Type), ("registered", type is not null)]);
                if (type is null)
                    throw new InvalidOperationException($"Outbox event type '{message.Type}' is not registered");
                var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(message.Payload, type)!;
                StepTrace.Step("SAL-DSP-03", "Deserialize the payload",
                    [("rowId", message.Id), ("eventType", integrationEvent.GetType().Name), ("saleId", SaleEventIds.SaleIdOf(integrationEvent))]);
                await _publisher.PublishAsync(integrationEvent, message.Id, message.Sequence, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception,
                    "Dispatch of outbox message {OutboxMessageId} ({OutboxMessageType}) failed; it and later messages wait for the next cycle",
                    message.Id, message.Type);
                StepTrace.Step("SAL-DSP-07", "Log the failure and end the cycle",
                    [("rowId", message.Id), ("sequence", message.Sequence), ("eventType", message.Type), ("error", exception), ("dispatched", dispatched)]);
                break;
            }

            message.ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync(cancellationToken);
            StepTrace.Step("SAL-DSP-05", "Set ProcessedAt and save the row",
                [("rowId", message.Id), ("sequence", message.Sequence), ("processedAt", message.ProcessedAt)]);
            dispatched++;
            StepTrace.Step("SAL-DSP-06", "More rows?", [("dispatched", dispatched), ("pending", pending.Count), ("more", dispatched < pending.Count)]);
        }

        return dispatched;
    }
}
