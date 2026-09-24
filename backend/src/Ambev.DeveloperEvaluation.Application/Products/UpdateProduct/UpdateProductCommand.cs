using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Command for updating an existing product.
/// </summary>
public class UpdateProductCommand : IRequest<UpdateProductResult>
{
    /// <summary>
    /// Gets or sets the unique identifier of the product to update.
    /// </summary>
    public Guid Id { get; set; }

    // Work item: FEAT-013
    /// <summary>
    /// Gets or sets the new product code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new unit price. Existing sale items keep the price they copied.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
