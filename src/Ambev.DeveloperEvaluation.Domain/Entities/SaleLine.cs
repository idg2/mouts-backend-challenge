namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TD-039
/// <summary>
/// One line of a sale as the aggregate receives it on create or update: the item it refers to (null for a new
/// item), the product with the description and unit price copied from the catalog or kept from the stored item, the
/// quantity, the discount the client asked for, and the cancelled flag. Only <see cref="Sale"/> turns lines into items.
/// </summary>
/// <param name="ItemId">The id of the existing item, or null for a new item</param>
/// <param name="ProductId">The product id (external identity)</param>
/// <param name="ProductDescription">The product description copy</param>
/// <param name="UnitPrice">The unit price copy</param>
/// <param name="Quantity">The quantity</param>
/// <param name="RequestedDiscountPercentage">The discount the client asked for, or null to receive the ceiling</param>
/// <param name="IsCancelled">Whether the line is cancelled</param>
public sealed record SaleLine(
    Guid? ItemId,
    Guid ProductId,
    string ProductDescription,
    decimal UnitPrice,
    int Quantity,
    decimal? RequestedDiscountPercentage,
    bool IsCancelled);
