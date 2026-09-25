using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// Records integration events in the outbox, inside the transaction of the command that raises them.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Stores an event in the outbox as part of the open transaction.
    /// </summary>
    /// <param name="integrationEvent">The event, whose type must be in <see cref="IntegrationEventTypes"/></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">No transaction is open</exception>
    Task EnqueueAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
