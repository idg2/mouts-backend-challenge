namespace Ambev.DeveloperEvaluation.Domain.Events.Sales;

// Work item: TASK-028 (FEAT-004), TASK-062 (FEAT-001)
/// <summary>
/// One sale item as carried by <see cref="SaleSnapshot"/>. The last three members were added with the discount
/// policies; a payload stored before them deserializes with null, an empty id, and zero.
/// </summary>
/// <param name="ItemId">The item id</param>
/// <param name="LineNumber">The line number within the sale</param>
/// <param name="ProductId">The product id</param>
/// <param name="ProductDescription">The product description copied into the sale</param>
/// <param name="UnitPrice">The unit price copied into the sale</param>
/// <param name="Quantity">The quantity</param>
/// <param name="DiscountPercentage">The applied discount percentage</param>
/// <param name="DiscountAmount">The discount amount</param>
/// <param name="TotalAmount">The item total</param>
/// <param name="IsCancelled">Whether the item is cancelled</param>
/// <param name="RequestedDiscountPercentage">The discount percentage the client asked for, or null</param>
/// <param name="DiscountPolicyId">The discount policy that priced the item</param>
/// <param name="DiscountCeilingPercentage">The highest percentage the policy allowed</param>
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
    bool IsCancelled,
    decimal? RequestedDiscountPercentage,
    Guid DiscountPolicyId,
    decimal DiscountCeilingPercentage);
