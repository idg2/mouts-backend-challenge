namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.GetProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// API response model for the GetProduct operation.
/// </summary>
public class GetProductResponse
{
    /// <summary>
    /// The unique identifier of the product.
    /// </summary>
    public Guid Id { get; set; }

    // Work item: FEAT-013
    /// <summary>
    /// The product code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
