using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Events;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// Contains unit tests for the <see cref="IntegrationEventTypes"/> registry.
/// </summary>
public class IntegrationEventTypesTests
{
    /// <summary>
    /// Tests that each of the five sale events is registered under its type name and found back by it.
    /// </summary>
    [Fact(DisplayName = "Given a sale event When resolving its name Then the name finds the same type")]
    public void Given_SaleEvent_When_ResolvingName_Then_NameFindsSameType()
    {
        // Arrange
        var snapshot = new SaleSnapshot(Guid.NewGuid(), 1, DateTime.UtcNow, Guid.NewGuid(), "Acme", Guid.NewGuid(), "Downtown", 0m, false, []);
        IIntegrationEvent[] events =
        [
            new SaleCreated(snapshot), new SaleModified(snapshot), new SaleCancelled(Guid.NewGuid()),
            new ItemCancelled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), new SaleDeleted(Guid.NewGuid())
        ];

        // Act
        var names = events.Select(IntegrationEventTypes.NameOf).ToList();

        // Assert
        names.Should().Equal("SaleCreated", "SaleModified", "SaleCancelled", "ItemCancelled", "SaleDeleted");
        names.Select(IntegrationEventTypes.Find).Should().Equal(events.Select(e => e.GetType()));
    }

    /// <summary>
    /// Tests that an event type outside the registry is rejected, so it can never reach the outbox.
    /// </summary>
    [Fact(DisplayName = "Given an unregistered event When resolving its name Then throws")]
    public void Given_UnregisteredEvent_When_ResolvingName_Then_Throws()
    {
        // Act
        var act = () => IntegrationEventTypes.NameOf(new UnregisteredEvent());

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*UnregisteredEvent*");
    }

    /// <summary>
    /// Tests that an unknown name, as a stored row could hold, finds no type.
    /// </summary>
    [Fact(DisplayName = "Given an unknown name When finding the type Then returns null")]
    public void Given_UnknownName_When_Finding_Then_ReturnsNull()
    {
        // Act
        var type = IntegrationEventTypes.Find("System.IO.File");

        // Assert
        type.Should().BeNull();
    }

    // Work item: TASK-028 (FEAT-004)
    private sealed record UnregisteredEvent : IIntegrationEvent;
}
