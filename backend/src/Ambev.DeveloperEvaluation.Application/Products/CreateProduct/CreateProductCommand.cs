using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Command for creating a new product.
/// </summary>
public class CreateProductCommand : IRequest<CreateProductResult>
{
    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
