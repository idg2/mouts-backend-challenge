using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.DevConsole.Trace;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepWaiter"/>.
/// </summary>
public class StepWaiterTests
{
    private static StepEvent Event(string key, params (string Name, string Value)[] values) =>
        new(DateTime.Now, 1, key, null, "t", values, "f.cs", 1);

    [Fact(DisplayName = "Given an event recorded before the wait When waiting Then it returns at once")]
    public async Task Given_EventBefore_When_Waiting_Then_ReturnsAtOnce()
    {
        // Arrange
        var waiter = new StepWaiter();
        waiter.Record(Event("SAL-ASY-07", ("messageId", "m1"), ("saleId", "s1")));

        // Act
        var found = await waiter.WaitAsync("SAL-ASY-07", TimeSpan.FromSeconds(1), ("saleId", "s1"));

        // Assert
        found.Should().NotBeNull();
        found!.Values.Should().Contain(("messageId", "m1"));
    }

    [Fact(DisplayName = "Given an event recorded after the wait starts When waiting Then it completes")]
    public async Task Given_EventAfter_When_Waiting_Then_Completes()
    {
        // Arrange
        var waiter = new StepWaiter();
        var wait = waiter.WaitAsync("SAL-CON-04", TimeSpan.FromSeconds(5), ("saleId", "s2"), ("eventType", "SaleDeleted"));

        // Act
        waiter.Record(Event("SAL-CON-04", ("saleId", "s2"), ("eventType", "SaleCreated")));
        waiter.Record(Event("SAL-CON-04", ("saleId", "s2"), ("eventType", "SaleDeleted")));

        // Assert
        (await wait).Should().NotBeNull();
        (await wait)!.Values.Should().Contain(("eventType", "SaleDeleted"));
    }

    [Fact(DisplayName = "Given a key never seen When waiting Then it times out with null")]
    public async Task Given_KeyNeverSeen_When_Waiting_Then_TimesOutWithMessage()
    {
        // Arrange
        var waiter = new StepWaiter();

        // Act
        var found = await waiter.WaitAsync("SAL-CON-04", TimeSpan.FromMilliseconds(50), ("saleId", "nope"));

        // Assert
        found.Should().BeNull();
    }

    [Fact(DisplayName = "Given a shared key When finding Then the shared key matches too")]
    public void Given_SharedKey_When_Finding_Then_Matches()
    {
        // Arrange
        var waiter = new StepWaiter();
        waiter.Record(new StepEvent(DateTime.Now, 1, "SAL-CRT-12", "CMN-TXN-06", "Commit", [("saleId", "s3")], "f.cs", 1));

        // Assert
        waiter.Find("CMN-TXN-06", ("saleId", "s3")).Should().NotBeNull();
        waiter.Find("SAL-CRT-12").Should().NotBeNull();
    }
}
