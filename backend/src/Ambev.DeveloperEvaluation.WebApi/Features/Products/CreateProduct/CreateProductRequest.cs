namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Represents a request to create a new product.
/// </summary>
public class CreateProductRequest
{
    // Work item: FEAT-013
    /// <summary>
    /// Gets or sets the product code. Required, at most 50 characters; stored trimmed and in upper case, and unique.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description. Required, at most 200 characters.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price. Must be greater than zero.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
