using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;

// Work item: TASK-022 (FEAT-010), TD-006
/// <summary>
/// Command for deleting a sale and its items.
/// </summary>
public record DeleteSaleCommand : IRequest<DeleteSaleResult>, ITransactionalCommand
{
    /// <summary>
    /// The unique identifier of the sale to delete.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of DeleteSaleCommand.
    /// </summary>
    /// <param name="id">The ID of the sale to delete</param>
    public DeleteSaleCommand(Guid id)
    {
        Id = id;
    }
}
