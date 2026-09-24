namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Response model for the ListSales operation: one page of sale headers and the total count.
/// </summary>
public class ListSalesResult
{
    /// <summary>
    /// The sales on the requested page, without items.
    /// </summary>
    public List<ListSalesItem> Items { get; set; } = [];

    /// <summary>
    /// The total number of sales.
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

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// A sale header in the ListSales result.
/// </summary>
public class ListSalesItem
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
}
