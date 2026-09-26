namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// Represents a request to create a new sale. The sale number, date, names, descriptions, unit prices, discounts, and
/// totals are filled by the server.
/// </summary>
public class CreateSaleRequest
{
    /// <summary>
    /// Gets or sets the customer id.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the branch id.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the sale items. At least one is required.
    /// </summary>
    public List<CreateSaleItemRequest> Items { get; set; } = [];
}
