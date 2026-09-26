namespace Ambev.DeveloperEvaluation.Domain.Services;

// Work item: TASK-064 (FEAT-001), TD-039
/// <summary>
/// One line of a create or update command, as the discount rules see it.
/// </summary>
/// <param name="ProductId">The product</param>
/// <param name="Quantity">The quantity, above zero</param>
/// <param name="RequestedDiscountPercentage">The discount the client asked for, or null for the ceiling</param>
/// <param name="IsCancelled">Whether the line is cancelled; a cancelled line counts toward no total</param>
public sealed record SaleDiscountLine(Guid ProductId, int Quantity, decimal? RequestedDiscountPercentage, bool IsCancelled);
