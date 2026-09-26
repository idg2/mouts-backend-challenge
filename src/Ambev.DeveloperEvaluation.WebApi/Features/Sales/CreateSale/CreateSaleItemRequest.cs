namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// One item of a <see cref="CreateSaleRequest"/>. The server computes the discount amount and the total from the
/// discount policies.
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

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Gets or sets the requested discount percentage, from 0 to 100; omit it to receive the ceiling of the product's
    /// total. A value above the ceiling is rejected with DiscountAboveAllowed.
    /// </summary>
    public decimal? DiscountPercentage { get; set; }
}
