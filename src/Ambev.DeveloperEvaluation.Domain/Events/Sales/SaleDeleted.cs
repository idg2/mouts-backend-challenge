namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// A sale was deleted.
/// </summary>
/// <param name="SaleId">The sale id</param>
public sealed record SaleDeleted(Guid SaleId) : IIntegrationEvent;
