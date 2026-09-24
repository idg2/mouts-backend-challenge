namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// One item of a <see cref="CreateSaleRequest"/>.
/// </summary>
public class CreateSaleItemRequest
{
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
}
