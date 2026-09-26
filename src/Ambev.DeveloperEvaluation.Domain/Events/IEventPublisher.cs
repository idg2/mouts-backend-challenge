namespace Ambev.DeveloperEvaluation.Domain.Events;

// Work item: TASK-030 (FEAT-004)
/// <summary>
/// Hands an integration event taken from the outbox to the message transport.
/// </summary>
public interface IEventPublisher
{
    // Work item: TASK-076 (FEAT-003)
    /// <summary>
    /// Publishes an event with a fixed id, so a re-sent event keeps its id and consumers can deduplicate, and with its
    /// outbox sequence, so a stateful consumer can order events of one sale.
    /// </summary>
    /// <param name="integrationEvent">The event</param>
    /// <param name="eventId">The outbox row id, used as the message id</param>
    /// <param name="sequence">The outbox row sequence, sent as the ordering header</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(IIntegrationEvent integrationEvent, Guid eventId, long sequence, CancellationToken cancellationToken);
}
