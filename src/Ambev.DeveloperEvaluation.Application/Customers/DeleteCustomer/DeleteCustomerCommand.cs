using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;

// Work item: TASK-018 (FEAT-010), TD-006
/// <summary>
/// Command for deleting a customer.
/// </summary>
public record DeleteCustomerCommand : IRequest<DeleteCustomerResult>, ITransactionalCommand
{
    /// <summary>
    /// The unique identifier of the customer to delete.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of DeleteCustomerCommand.
    /// </summary>
    /// <param name="id">The ID of the customer to delete</param>
    public DeleteCustomerCommand(Guid id)
    {
        Id = id;
    }
}
