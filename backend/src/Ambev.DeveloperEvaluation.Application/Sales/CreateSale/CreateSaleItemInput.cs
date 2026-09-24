namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// One item of a <see cref="CreateSaleCommand"/>.
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
}
