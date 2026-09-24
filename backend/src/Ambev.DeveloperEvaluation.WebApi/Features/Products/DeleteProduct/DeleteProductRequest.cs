namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.DeleteProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Request model for deleting a product.
/// </summary>
public class DeleteProductRequest
{
    /// <summary>
    /// The unique identifier of the product to delete.
    /// </summary>
    public Guid Id { get; set; }
}
