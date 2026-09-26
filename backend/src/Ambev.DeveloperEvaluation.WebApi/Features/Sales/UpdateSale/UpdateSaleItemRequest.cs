namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// One item of an <see cref="UpdateSaleRequest"/>. The server computes the discount amount and the total from the
/// discount policies in effect at the sale date.
/// </summary>
public class UpdateSaleItemRequest
{
    /// <summary>
    /// Gets or sets the id of an existing item of the sale; omit it (or send null) for a new item.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the product id.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity. Must be greater than zero.
    /// </summary>
    public int Quantity { get; set; }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Gets or sets the requested discount percentage, from 0 to 100; omit it to receive the ceiling of the product's
    /// total. A value above the ceiling is rejected with DiscountAboveAllowed.
    /// </summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
