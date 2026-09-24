namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Represents a request to create a new customer.
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>
    /// Gets or sets the customer name. Required, at most 100 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
