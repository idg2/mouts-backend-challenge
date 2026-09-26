namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// One item of an <see cref="UpdateSaleCommand"/>. The discount amount and the total are computed from the discount
/// policies.
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

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Gets or sets the requested discount percentage, or null to receive the ceiling of the product's total.
    /// </summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
