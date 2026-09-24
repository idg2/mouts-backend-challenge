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
}
