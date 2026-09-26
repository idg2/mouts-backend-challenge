namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

// Work item: TASK-021 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// API response model for a sale with its items, returned by create, get, and update.
/// </summary>
public class SaleResponse
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

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// The sale total: the sum of the totals of the active items.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// The sale items.
    /// </summary>
    public List<SaleItemResponse> Items { get; set; } = [];
}
