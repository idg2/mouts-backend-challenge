namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// API response model for the UpdateCustomer operation.
/// </summary>
public class UpdateCustomerResponse
{
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // Work item: FEAT-012
    /// <summary>
    /// Gets or sets the customer's CPF (11 characters) or CNPJ (14 characters), without mask.
    /// </summary>
    public string Document { get; set; } = string.Empty;
}
