namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// API response model for one sale header in the ListSales page (no items).
/// </summary>
public class ListSalesResponse
{
    /// <summary>
    /// The unique identifier of the sale.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The sequential sale number.
    /// </summary>
    public long SaleNumber { get; set; }

    /// <summary>
    /// The UTC date and time when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// The customer id.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The customer name copied into the sale.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// The branch id.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// The branch name copied into the sale.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    // Work item: TASK-066 (FEAT-001)
    /// <summary>
    /// The sale total, computed from the active items.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
