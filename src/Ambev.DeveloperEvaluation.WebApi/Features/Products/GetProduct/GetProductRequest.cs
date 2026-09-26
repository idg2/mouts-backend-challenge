namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.GetProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Request model for getting a product by ID.
/// </summary>
public class GetProductRequest
{
    /// <summary>
    /// The unique identifier of the product to retrieve.
    /// </summary>
    public Guid Id { get; set; }
}
