namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Represents a request to update a sale. The item list is complete: items sent with an id are updated,
/// items without id are added, and existing items that are not sent are removed.
/// </summary>
public class UpdateSaleRequest
{
    /// <summary>
    /// Gets or sets the sale id. The controller sets it from the route.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the complete item list. At least one item is required.
    /// </summary>
    public List<UpdateSaleItemRequest> Items { get; set; } = [];
}
