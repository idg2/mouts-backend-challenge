namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Represents a request to update a product.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Gets or sets the product id. The controller sets it from the route.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new product description. Required, at most 200 characters.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new unit price. Must be greater than zero.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
