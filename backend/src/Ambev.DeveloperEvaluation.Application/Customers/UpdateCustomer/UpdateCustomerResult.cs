namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Represents the customer returned after a successful update.
/// </summary>
public class UpdateCustomerResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the customer.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
