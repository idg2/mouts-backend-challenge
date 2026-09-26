#if DEBUG
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using NSubstitute;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Transport;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-055 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceOutgoingStep"/> Rebus step.
/// </summary>
[Collection("StepTrace")]
public class StepTraceOutgoingStepTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public StepTraceOutgoingStepTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given a queued command When sending Then BUS-01 carries the message id and the sale id, and next runs once")]
    public async Task Given_Command_When_Sending_Then_Bus01WithSaleId()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var calls = 0;
        var context = Context(new CreateSaleCommand { Id = saleId }, "msg-1");

        // Act
        await new StepTraceOutgoingStep().Process(context, () =>
        {
            calls++;
            return Task.CompletedTask;
        });

        // Assert
        calls.Should().Be(1);
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().Be("SAL-BUS-01");
        stepEvent.Values.Should().Contain(("messageId", "msg-1")).And.Contain(("saleId", saleId.ToString()))
            .And.Contain(("eventType", nameof(CreateSaleCommand))).And.Contain(("queue", "sales-intake"));
    }

    [Fact(DisplayName = "Given a sale event When sending Then BUS-01 carries the event's sale id and type")]
    public async Task Given_Event_When_Sending_Then_Bus01WithEventSaleId()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var context = Context(new SaleCancelled(saleId), "msg-2");

        // Act
        await new StepTraceOutgoingStep().Process(context, () => Task.CompletedTask);

        // Assert
        _events.Should().ContainSingle().Which.Values.Should()
            .Contain(("saleId", saleId.ToString())).And.Contain(("eventType", nameof(SaleCancelled)));
    }

    private static OutgoingStepContext Context(object body, string messageId) =>
        new(new Message(new Dictionary<string, string> { [Headers.MessageId] = messageId }, body),
            Substitute.For<ITransactionContext>(),
            new DestinationAddresses(["sales-intake"]));
}
#endif
