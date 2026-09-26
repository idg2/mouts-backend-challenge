#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using NSubstitute;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Transport;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-055 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceErrorHandler"/> decorator.
/// </summary>
[Collection("StepTrace")]
public class StepTraceErrorHandlerTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public StepTraceErrorHandlerTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given a poison message When handling Then BUS-07 prints and the inner handler runs")]
    public async Task Given_PoisonMessage_When_Handling_Then_Bus07AndDelegate()
    {
        // Arrange
        var inner = Substitute.For<IErrorHandler>();
        var transaction = Substitute.For<ITransactionContext>();
        var message = new TransportMessage(new Dictionary<string, string> { [Headers.MessageId] = "msg-9", [Headers.Type] = "X, Y" }, Array.Empty<byte>());
        var exception = new ExceptionInfo("InvalidOperationException", "boom", "details", DateTimeOffset.UtcNow);

        // Act
        await new StepTraceErrorHandler(inner).HandlePoisonMessage(message, transaction, exception);

        // Assert
        await inner.Received(1).HandlePoisonMessage(message, transaction, exception);
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().Be("SAL-BUS-07");
        stepEvent.Values.Should().Contain(("messageId", "msg-9")).And.Contain(("error", "InvalidOperationException"));
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given assembly-qualified type names When handling Then BUS-07 prints the simple names")]
    public async Task Given_AssemblyQualifiedNames_When_Handling_Then_SimpleNames()
    {
        // Arrange
        var inner = Substitute.For<IErrorHandler>();
        var transaction = Substitute.For<ITransactionContext>();
        var message = new TransportMessage(new Dictionary<string, string>
        {
            [Headers.MessageId] = "msg-10",
            [Headers.Type] = "Ambev.DeveloperEvaluation.Application.Sales.CreateSale.CreateSaleCommand, Ambev.DeveloperEvaluation.Application"
        }, Array.Empty<byte>());
        var exception = new ExceptionInfo("System.AggregateException, mscorlib", "1 unhandled exceptions", "details", DateTimeOffset.UtcNow);

        // Act
        await new StepTraceErrorHandler(inner).HandlePoisonMessage(message, transaction, exception);

        // Assert
        await inner.Received(1).HandlePoisonMessage(message, transaction, exception);
        _events.Should().ContainSingle().Which.Values.Should()
            .Contain(("eventType", "CreateSaleCommand"))
            .And.Contain(("error", "AggregateException"))
            .And.Contain(("reason", "1 unhandled exceptions"));
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given no sink When handling Then nothing is traced and the inner handler runs")]
    public async Task Given_NoSink_When_Handling_Then_InnerRuns()
    {
        // Arrange
        StepTrace.Sink = null;
        var inner = Substitute.For<IErrorHandler>();
        var transaction = Substitute.For<ITransactionContext>();
        var message = new TransportMessage(new Dictionary<string, string> { [Headers.MessageId] = "msg-11" }, Array.Empty<byte>());
        var exception = new ExceptionInfo("T", "m", "d", DateTimeOffset.UtcNow);

        // Act
        await new StepTraceErrorHandler(inner).HandlePoisonMessage(message, transaction, exception);

        // Assert
        await inner.Received(1).HandlePoisonMessage(message, transaction, exception);
        _events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given a poison message without an id When handling Then BUS-07 prints and the inner handler still runs")]
    public async Task Given_PoisonMessageWithoutId_When_Handling_Then_InnerStillRuns()
    {
        // Arrange
        var inner = Substitute.For<IErrorHandler>();
        var transaction = Substitute.For<ITransactionContext>();
        var message = new TransportMessage(new Dictionary<string, string>(), Array.Empty<byte>());
        var exception = new ExceptionInfo("RebusApplicationException", "no id", "details", DateTimeOffset.UtcNow);

        // Act
        await new StepTraceErrorHandler(inner).HandlePoisonMessage(message, transaction, exception);

        // Assert
        await inner.Received(1).HandlePoisonMessage(message, transaction, exception);
        _events.Should().ContainSingle().Which.Values.Should().Contain(("messageId", "null"));
    }
}
#endif
