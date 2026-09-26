using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using NSubstitute;
using Rebus.Bus;
using Rebus.Messages;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging;

// Work item: TASK-031 (FEAT-004)
/// <summary>
/// Contains unit tests for the <see cref="RebusEventPublisher"/> class.
/// </summary>
public class RebusEventPublisherTests
{
    // Work item: TASK-076 (FEAT-003)
    /// <summary>
    /// Tests that the event is sent to the local queue with the outbox row id as its message id and the outbox
    /// sequence as the ordering header.
    /// </summary>
    [Fact(DisplayName = "Given an event, its id, and its sequence When publishing Then sends it locally with both headers")]
    public async Task Given_EventIdAndSequence_When_Publishing_Then_SendsLocallyWithBothHeaders()
    {
        // Arrange
        var bus = Substitute.For<IBus>();
        var integrationEvent = new SaleDeleted(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Act
        await new RebusEventPublisher(bus).PublishAsync(integrationEvent, eventId, 77, CancellationToken.None);

        // Assert
        await bus.Received(1).SendLocal(
            integrationEvent,
            Arg.Is<IDictionary<string, string>>(headers =>
                headers[Headers.MessageId] == eventId.ToString() && headers[RebusEventPublisher.SequenceHeader] == "77"));
    }
}
