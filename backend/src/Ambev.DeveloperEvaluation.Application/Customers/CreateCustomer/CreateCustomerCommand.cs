using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010), TD-006
/// <summary>
/// Command for creating a new customer.
/// </summary>
public class CreateCustomerCommand : IRequest<CreateCustomerResult>, ITransactionalCommand
{
    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
