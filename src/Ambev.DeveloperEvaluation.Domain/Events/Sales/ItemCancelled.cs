namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// An existing sale item went from active to cancelled.
/// </summary>
/// <param name="SaleId">The sale id</param>
/// <param name="ItemId">The item id</param>
/// <param name="ProductId">The item's product id</param>
public sealed record ItemCancelled(Guid SaleId, Guid ItemId, Guid ProductId) : IIntegrationEvent;
