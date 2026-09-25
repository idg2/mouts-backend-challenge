using Ambev.DeveloperEvaluation.Domain.Events.Sales;

namespace Ambev.DeveloperEvaluation.Domain.Events;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// The fixed list of events the outbox accepts, keyed by type name. Stored rows are resolved only through it,
/// never through <see cref="Type.GetType(string)"/>.
/// </summary>
public static class IntegrationEventTypes
{
    private static readonly IReadOnlyDictionary<string, Type> TypesByName = new[]
        {
            typeof(SaleCreated), typeof(SaleModified), typeof(SaleCancelled), typeof(ItemCancelled), typeof(SaleDeleted)
        }
        .ToDictionary(type => type.Name);

    /// <summary>
    /// Returns the registered name of an event.
    /// </summary>
    /// <param name="integrationEvent">The event</param>
    /// <returns>The event's type name</returns>
    /// <exception cref="ArgumentException">The event's type is not registered</exception>
    public static string NameOf(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();
        return TypesByName.TryGetValue(type.Name, out var registered) && registered == type
            ? type.Name
            : throw new ArgumentException(
                $"Integration event type {type.FullName} is not registered in {nameof(IntegrationEventTypes)}",
                nameof(integrationEvent));
    }

    /// <summary>
    /// Finds the event type registered under a name.
    /// </summary>
    /// <param name="name">The stored type name</param>
    /// <returns>The type, or null when the name is not registered</returns>
    public static Type? Find(string name) => TypesByName.GetValueOrDefault(name);
}
