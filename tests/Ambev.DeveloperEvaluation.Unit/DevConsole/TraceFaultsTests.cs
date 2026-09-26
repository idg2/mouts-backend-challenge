using Ambev.DeveloperEvaluation.DevConsole.Trace;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TD-030
/// <summary>
/// Contains unit tests for the faults the trace console injects: each one fails once when armed and never otherwise.
/// </summary>
public class TraceFaultsTests
{
    // Work item: TD-030
    [Fact(DisplayName = "Given an armed relay fault When two cycles run Then only the first fails")]
    public async Task Given_ArmedRelayFault_When_TwoCyclesRun_Then_OnlyTheFirstFails()
    {
        // Arrange
        var faults = new TraceFaults();
        var inner = Substitute.For<IOutboxRelay>();
        inner.DispatchPendingAsync(10, Arg.Any<CancellationToken>()).Returns(3);
        var relay = new FaultyOutboxRelay(inner, faults);
        faults.FailNextRelayCycle();

        // Act
        var first = () => relay.DispatchPendingAsync(10, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(first);
        var second = await relay.DispatchPendingAsync(10, CancellationToken.None);

        // Assert
        second.Should().Be(3);
        await inner.Received(1).DispatchPendingAsync(10, Arg.Any<CancellationToken>());
    }

    // Work item: TD-030
    [Fact(DisplayName = "Given no armed relay fault When a cycle runs Then it reaches the relay")]
    public async Task Given_NoRelayFault_When_CycleRuns_Then_RelayRuns()
    {
        // Arrange
        var inner = Substitute.For<IOutboxRelay>();
        inner.DispatchPendingAsync(10, Arg.Any<CancellationToken>()).Returns(2);
        var relay = new FaultyOutboxRelay(inner, new TraceFaults());

        // Act
        var dispatched = await relay.DispatchPendingAsync(10, CancellationToken.None);

        // Assert
        dispatched.Should().Be(2);
    }

    // Work item: TD-030
    [Fact(DisplayName = "Given an armed request fault When two actions run Then only the first throws")]
    public void Given_ArmedRequestFault_When_TwoActionsRun_Then_OnlyTheFirstThrows()
    {
        // Arrange
        var faults = new TraceFaults();
        var filter = new FailingRequestFilter(faults);
        faults.FailNextRequest();

        // Act
        var first = () => filter.OnActionExecuting(ExecutingContext());
        var second = () => filter.OnActionExecuting(ExecutingContext());

        // Assert
        first.Should().Throw<InvalidOperationException>();
        second.Should().NotThrow();
    }

    private static ActionExecutingContext ExecutingContext() =>
        new(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
}
