namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// One item of a <see cref="CreateSaleCommand"/>. The discount amount and the total are computed from the discount
/// policies.
/// </summary>
public class CreateSaleItemInput
{
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
}
