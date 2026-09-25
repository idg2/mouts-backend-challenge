using Ambev.DeveloperEvaluation.Domain.Events;
using Rebus.Bus;
using Rebus.Messages;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-031 (FEAT-004)
/// <summary>
/// Implementation of IEventPublisher over Rebus: sends the event to the API's own input queue with the outbox row
/// id as the message id.
/// </summary>
public class RebusEventPublisher : IEventPublisher
{
    private readonly IBus _bus;

    /// <summary>
    /// Initializes a new instance of RebusEventPublisher
    /// </summary>
    /// <param name="bus">The Rebus bus</param>
    public RebusEventPublisher(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public Task PublishAsync(IIntegrationEvent integrationEvent, Guid eventId, CancellationToken cancellationToken) =>
        _bus.SendLocal(integrationEvent, new Dictionary<string, string> { [Headers.MessageId] = eventId.ToString() });
}
