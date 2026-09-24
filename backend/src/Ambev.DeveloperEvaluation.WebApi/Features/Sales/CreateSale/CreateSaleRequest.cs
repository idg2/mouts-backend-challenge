namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Represents a request to create a new sale. The sale number, date, names, descriptions, and unit prices
/// are filled by the server.
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
    /// Gets or sets the sale total, stored as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the sale items. At least one is required.
    /// </summary>
    public List<CreateSaleItemRequest> Items { get; set; } = [];
}
