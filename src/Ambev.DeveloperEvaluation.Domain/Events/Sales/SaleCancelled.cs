namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// A sale went from active to cancelled.
/// </summary>
/// <param name="SaleId">The sale id</param>
public sealed record SaleCancelled(Guid SaleId) : IIntegrationEvent;
