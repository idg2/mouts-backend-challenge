namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.GetCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Request model for getting a customer by ID.
/// </summary>
public class GetCustomerRequest
{
    /// <summary>
    /// The unique identifier of the customer to retrieve.
    /// </summary>
    public Guid Id { get; set; }
}
