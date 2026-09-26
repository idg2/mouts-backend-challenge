namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// A sale was updated.
/// </summary>
/// <param name="Sale">The sale after the update</param>
public sealed record SaleModified(SaleSnapshot Sale) : IIntegrationEvent;
