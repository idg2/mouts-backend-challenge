using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Command for retrieving a product by ID.
/// </summary>
public record GetProductCommand : IRequest<GetProductResult>
{
    /// <summary>
    /// The unique identifier of the product to retrieve.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of GetProductCommand.
    /// </summary>
    /// <param name="id">The ID of the product to retrieve</param>
    public GetProductCommand(Guid id)
    {
        Id = id;
    }
}
