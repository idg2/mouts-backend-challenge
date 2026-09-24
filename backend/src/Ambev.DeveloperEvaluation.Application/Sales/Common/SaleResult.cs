namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// A sale with its items, returned by the create, get, and update operations.
/// </summary>
public class SaleResult
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

    /// <summary>
    /// The sale total, as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// The sale items.
    /// </summary>
    public List<SaleItemResult> Items { get; set; } = [];
}
