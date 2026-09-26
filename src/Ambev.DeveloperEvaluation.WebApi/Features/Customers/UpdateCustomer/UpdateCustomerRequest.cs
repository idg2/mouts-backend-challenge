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

    // Work item: FEAT-012
    /// <summary>
    /// Gets or sets the customer's CPF or CNPJ; a mask ('.', '-', '/') is accepted and removed.
    /// </summary>
    public string Document { get; set; } = string.Empty;
}
