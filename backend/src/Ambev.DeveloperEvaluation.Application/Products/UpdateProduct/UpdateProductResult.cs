namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Represents the product returned after a successful update.
/// </summary>
public class UpdateProductResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the product.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
