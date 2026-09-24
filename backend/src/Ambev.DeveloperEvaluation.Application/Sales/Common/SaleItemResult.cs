namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// One item of a <see cref="SaleResult"/>.
/// </summary>
public class SaleItemResult
{
    /// <summary>
    /// The unique identifier of the item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The product id.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// The product description copied into the item.
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// The unit price copied into the item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// The quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The discount percentage, as received.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// The discount amount, as received.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// The item total, as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
