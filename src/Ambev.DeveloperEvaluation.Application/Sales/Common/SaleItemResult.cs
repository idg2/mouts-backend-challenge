namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

// Work item: TASK-021 (FEAT-010), TASK-064 (FEAT-001), TASK-066 (FEAT-001)
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

    // Work item: TASK-066 (FEAT-001)
    /// <summary>
    /// The discount the client asked for, in percent, or null when the ceiling was applied.
    /// </summary>
    public decimal? RequestedDiscountPercentage { get; set; }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// The applied discount percentage.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// The id of the discount policy that priced the item.
    /// </summary>
    public Guid DiscountPolicyId { get; set; }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// The highest discount percentage the policy allowed for the product's total.
    /// </summary>
    public decimal DiscountCeilingPercentage { get; set; }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// The discount amount, computed from the discount policy.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// The item total, computed from the discount policy.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
