namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// A sale was created.
/// </summary>
/// <param name="Sale">The created sale</param>
public sealed record SaleCreated(SaleSnapshot Sale) : IIntegrationEvent;
