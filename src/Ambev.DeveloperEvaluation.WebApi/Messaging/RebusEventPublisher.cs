using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
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

    // Work item: TASK-076 (FEAT-003)
    /// <summary>
    /// The header that carries the outbox sequence of the event.
    /// </summary>
    public const string SequenceHeader = "outbox-sequence";

    // Work item: TASK-054 (FEAT-017), TASK-076 (FEAT-003)
    /// <inheritdoc />
    public Task PublishAsync(IIntegrationEvent integrationEvent, Guid eventId, long sequence, CancellationToken cancellationToken)
    {
        StepTrace.Step("SAL-DSP-04", "Send with the message id set to the row id",
            [("messageId", eventId), ("rowId", eventId), ("sequence", sequence), ("eventType", integrationEvent.GetType().Name), ("saleId", SaleEventIds.SaleIdOf(integrationEvent))]);
        return _bus.SendLocal(integrationEvent, new Dictionary<string, string>
        {
            [Headers.MessageId] = eventId.ToString(),
            [SequenceHeader] = sequence.ToString(CultureInfo.InvariantCulture)
        });
    }
}
