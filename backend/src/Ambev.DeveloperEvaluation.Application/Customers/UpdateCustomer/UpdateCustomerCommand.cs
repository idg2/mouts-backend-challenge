using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010), TD-006
/// <summary>
/// Command for updating an existing customer.
/// </summary>
public class UpdateCustomerCommand : IRequest<UpdateCustomerResult>, ITransactionalCommand
{
    /// <summary>
    /// Gets or sets the unique identifier of the customer to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
