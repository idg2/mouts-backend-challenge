namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Represents a request to update a customer.
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>
    /// Gets or sets the customer id. The controller sets it from the route.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new customer name. Required, at most 100 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
