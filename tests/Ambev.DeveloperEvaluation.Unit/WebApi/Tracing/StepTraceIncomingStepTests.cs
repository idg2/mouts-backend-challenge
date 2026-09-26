#if DEBUG
using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using NSubstitute;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Retry;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;
using Rebus.Transport;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-055 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceIncomingStep"/> Rebus step.
/// </summary>
[Collection("StepTrace")]
public class StepTraceIncomingStepTests : IDisposable
{
    private readonly List<StepEvent> _events = new();
    private readonly IErrorTracker _errorTracker = Substitute.For<IErrorTracker>();
    private readonly IFailFastChecker _failFastChecker = Substitute.For<IFailFastChecker>();
    private readonly ITransactionContext _transaction = Substitute.For<ITransactionContext>();
    private Func<ITransactionContext, Task>? _ack;

    public StepTraceIncomingStepTests()
    {
        StepTrace.Sink = _events.Add;
        _errorTracker.GetExceptions(Arg.Any<string>()).Returns(Task.FromResult<IReadOnlyList<ExceptionInfo>>(Array.Empty<ExceptionInfo>()));
        // The IncomingStepContext constructor stores itself in the transaction's Items.
        _transaction.Items.Returns(new ConcurrentDictionary<string, object>());
        _transaction.When(t => t.OnAck(Arg.Any<Func<ITransactionContext, Task>>()))
            .Do(call => _ack = call.Arg<Func<ITransactionContext, Task>>());
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given a queued command handled When processing Then lease, dispatch, and ack lines carry the sale id")]
    public async Task Given_HandledCommand_When_Processing_Then_LeaseDispatchAndAckLines()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var context = Context(new CreateSaleCommand { Id = saleId }, "msg-1");
        var step = CreateStep();

        // Act
        await step.Process(context, () => Task.CompletedTask);
        await _ack!(_transaction);

        // Assert
        _events.Select(e => e.Key).Should().Equal("SAL-BUS-02", "SAL-ASY-04", "SAL-BUS-03", "SAL-BUS-04", "SAL-ASY-07");
        _events.Should().AllSatisfy(e => e.Values.Should().Contain(("messageId", "msg-1")));
        _events.Single(e => e.Key == "SAL-ASY-07").Values.Should().Contain(("saleId", saleId.ToString()));
        _events.Single(e => e.Key == "SAL-BUS-02").Values.Should().Contain(("attempt", "1"));
    }

    [Fact(DisplayName = "Given an event handled When processing Then the consumer lines are used")]
    public async Task Given_HandledEvent_When_Processing_Then_ConsumerLines()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var context = Context(new SaleDeleted(saleId), "msg-2");

        // Act
        await CreateStep().Process(context, () => Task.CompletedTask);
        await _ack!(_transaction);

        // Assert
        _events.Select(e => e.Key).Should().Equal("SAL-BUS-02", "SAL-CON-01", "SAL-BUS-03", "SAL-BUS-04", "SAL-CON-04");
        _events.Single(e => e.Key == "SAL-CON-04").Values.Should()
            .Contain(("saleId", saleId.ToString())).And.Contain(("eventType", nameof(SaleDeleted)));
    }

    [Fact(DisplayName = "Given a fail-fast exception When processing Then BUS-05 prints, no ack line, and the exception rethrows")]
    public async Task Given_FailFast_When_Processing_Then_Bus05AndRethrow()
    {
        // Arrange
        var exception = new FluentValidation.ValidationException("bad");
        _failFastChecker.ShouldFailFast("msg-3", exception).Returns(true);
        var context = Context(new CreateSaleCommand { Id = Guid.NewGuid() }, "msg-3");

        // Act
        var act = () => CreateStep().Process(context, () => throw exception);

        // Assert
        (await act.Should().ThrowAsync<FluentValidation.ValidationException>()).Which.Should().BeSameAs(exception);
        _events.Select(e => e.Key).Should().Equal("SAL-BUS-02", "SAL-ASY-04", "SAL-BUS-03", "SAL-ASY-08", "SAL-BUS-05");
        _events.Single(e => e.Key == "SAL-ASY-08").Values.Should().Contain(("failFast", "True"));
        await _ack!(_transaction);
        _events.Should().NotContain(e => e.Key == "SAL-BUS-04");
    }

    [Fact(DisplayName = "Given a retryable exception on the third delivery When processing Then BUS-06 prints attempt 3 of 5")]
    public async Task Given_RetryableFailure_When_Processing_Then_Bus06WithAttempt()
    {
        // Arrange
        var exception = new InvalidOperationException("db down");
        _errorTracker.GetExceptions("msg-4").Returns(Task.FromResult<IReadOnlyList<ExceptionInfo>>(
            [new ExceptionInfo("T", "m", "d", DateTimeOffset.UtcNow), new ExceptionInfo("T", "m", "d", DateTimeOffset.UtcNow)]));
        var context = Context(new SaleDeleted(Guid.NewGuid()), "msg-4");

        // Act
        var act = () => CreateStep().Process(context, () => throw exception);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        var retried = _events.Single(e => e.Key == "SAL-BUS-06");
        retried.Values.Should().Contain(("attempt", "3")).And.Contain(("maxAttempts", "5")).And.Contain(("final", "False"));
        _events.Should().NotContain(e => e.Key == "SAL-BUS-05");
    }

    [Fact(DisplayName = "Given a retryable exception on the last delivery When processing Then BUS-06 is final")]
    public async Task Given_RetryableFailureOnLastDelivery_When_Processing_Then_Bus06Final()
    {
        // Arrange
        var info = new ExceptionInfo("T", "m", "d", DateTimeOffset.UtcNow);
        _errorTracker.GetExceptions("msg-5").Returns(Task.FromResult<IReadOnlyList<ExceptionInfo>>([info, info, info, info]));
        var context = Context(new SaleDeleted(Guid.NewGuid()), "msg-5");

        // Act
        var act = () => CreateStep().Process(context, () => throw new InvalidOperationException("db down"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _events.Single(e => e.Key == "SAL-BUS-06").Values.Should().Contain(("attempt", "5")).And.Contain(("final", "True"));
    }

    [Fact(DisplayName = "Given a handled message When processing Then next runs exactly once")]
    public async Task Given_HandledMessage_When_Processing_Then_NextRunsOnce()
    {
        // Arrange
        var calls = 0;
        var context = Context(new SaleDeleted(Guid.NewGuid()), "msg-6");

        // Act
        await CreateStep().Process(context, () =>
        {
            calls++;
            return Task.CompletedTask;
        });

        // Assert
        calls.Should().Be(1);
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given no sink When processing Then next runs once and the error tracker is not asked")]
    public async Task Given_NoSink_When_Processing_Then_NextOnceAndNoTrackerCall()
    {
        // Arrange
        StepTrace.Sink = null;
        var calls = 0;
        var context = Context(new CreateSaleCommand { Id = Guid.NewGuid() }, "msg-7");

        // Act
        await CreateStep().Process(context, () =>
        {
            calls++;
            return Task.CompletedTask;
        });

        // Assert
        calls.Should().Be(1);
        await _errorTracker.DidNotReceive().GetExceptions(Arg.Any<string>());
        _transaction.DidNotReceive().OnAck(Arg.Any<Func<ITransactionContext, Task>>());
        _events.Should().BeEmpty();
    }

    private StepTraceIncomingStep CreateStep() =>
        new(_errorTracker, _failFastChecker, new RetryStrategySettings(maxDeliveryAttempts: 5));

    private IncomingStepContext Context(object body, string messageId)
    {
        var headers = new Dictionary<string, string> { [Headers.MessageId] = messageId };
        var context = new IncomingStepContext(new TransportMessage(headers, Array.Empty<byte>()), _transaction);
        var message = new Message(headers, body);
        context.Save(message);
        context.Save(new HandlerInvokers(message, Array.Empty<HandlerInvoker>()));
        return context;
    }
}
#endif
