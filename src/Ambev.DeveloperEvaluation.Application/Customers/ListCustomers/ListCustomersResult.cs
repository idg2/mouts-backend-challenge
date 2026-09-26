namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Response model for the ListCustomers operation: one page of customers and the total count.
/// </summary>
public class ListCustomersResult
{
    /// <summary>
    /// The customers on the requested page.
    /// </summary>
    public List<ListCustomersItem> Items { get; set; } = [];

    /// <summary>
    /// The total number of customers.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    public int Size { get; set; }
}

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// A customer entry in the ListCustomers result.
/// </summary>
public class ListCustomersItem
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
