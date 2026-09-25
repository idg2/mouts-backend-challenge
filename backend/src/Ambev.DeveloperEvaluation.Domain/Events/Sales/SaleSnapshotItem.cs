namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// One sale item as carried by <see cref="SaleSnapshot"/>.
/// </summary>
/// <param name="ItemId">The item id</param>
/// <param name="LineNumber">The line number within the sale</param>
/// <param name="ProductId">The product id</param>
/// <param name="ProductDescription">The product description copied into the sale</param>
/// <param name="UnitPrice">The unit price copied into the sale</param>
/// <param name="Quantity">The quantity</param>
/// <param name="DiscountPercentage">The discount percentage</param>
/// <param name="DiscountAmount">The discount amount</param>
/// <param name="TotalAmount">The item total</param>
/// <param name="IsCancelled">Whether the item is cancelled</param>
public sealed record SaleSnapshotItem(
    Guid ItemId,
    int LineNumber,
    Guid ProductId,
    string ProductDescription,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TotalAmount,
    bool IsCancelled);
