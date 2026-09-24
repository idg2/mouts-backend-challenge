namespace Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Represents the customer returned after a successful creation.
/// </summary>
public class CreateCustomerResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the created customer.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
