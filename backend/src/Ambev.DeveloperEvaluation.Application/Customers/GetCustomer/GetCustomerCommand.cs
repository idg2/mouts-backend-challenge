using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Command for retrieving a customer by ID.
/// </summary>
public record GetCustomerCommand : IRequest<GetCustomerResult>
{
    /// <summary>
    /// The unique identifier of the customer to retrieve.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of GetCustomerCommand.
    /// </summary>
    /// <param name="id">The ID of the customer to retrieve</param>
    public GetCustomerCommand(Guid id)
    {
        Id = id;
    }
}
