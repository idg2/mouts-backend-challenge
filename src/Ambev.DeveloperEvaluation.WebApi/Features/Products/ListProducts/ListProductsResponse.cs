namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// API response model for one product in the ListProducts page.
/// </summary>
public class ListProductsResponse
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
