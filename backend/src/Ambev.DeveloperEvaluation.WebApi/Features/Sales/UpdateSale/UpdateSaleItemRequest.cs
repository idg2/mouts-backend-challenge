namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// One item of an <see cref="UpdateSaleRequest"/>.
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

    /// <summary>
    /// Gets or sets the discount percentage, from 0 to 100, stored as received.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the discount amount, stored as received.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the item total, stored as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
