namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// API response model for one customer in the ListCustomers page.
/// </summary>
public class ListCustomersResponse
{
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
