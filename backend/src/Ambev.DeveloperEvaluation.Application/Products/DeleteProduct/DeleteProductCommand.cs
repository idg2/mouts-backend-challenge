using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Command for deleting a product. Sales that reference it keep their own copy.
/// </summary>
public record DeleteProductCommand : IRequest<DeleteProductResult>
{
    /// <summary>
    /// The unique identifier of the product to delete.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of DeleteProductCommand.
    /// </summary>
    /// <param name="id">The ID of the product to delete</param>
    public DeleteProductCommand(Guid id)
    {
        Id = id;
    }
}
