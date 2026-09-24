using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Command for retrieving one page of customers ordered by name.
/// </summary>
public class ListCustomersCommand : IRequest<ListCustomersResult>
{
    /// <summary>
    /// Gets or sets the page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;
}
