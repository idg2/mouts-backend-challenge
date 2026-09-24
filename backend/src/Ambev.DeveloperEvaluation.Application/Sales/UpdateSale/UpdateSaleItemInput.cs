namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// One item of an <see cref="UpdateSaleCommand"/>.
/// </summary>
public class UpdateSaleItemInput
{
    /// <summary>
    /// Gets or sets the id of an existing item of the sale, or null for a new item.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the product id.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage, stored as received.
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
