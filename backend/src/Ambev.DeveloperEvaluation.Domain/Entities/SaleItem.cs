using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-015 (FEAT-010), TASK-062 (FEAT-001)
/// <summary>
/// Represents one line of a sale. The product is referenced by id; its description and unit price
/// are copies taken when the line was created or its product changed. The discount fields are a snapshot of the
/// discount policy that priced the line (<see cref="Sale.ApplyDiscounts"/>).
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>
    /// Gets or sets the id of the sale this item belongs to.
    /// </summary>
    public Guid SaleId { get; set; }

    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Gets or sets the position of the item in the sale, starting at 1, in the order the client sent the items.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the product id (external identity, no foreign key).
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product description copied from the catalog.
    /// </summary>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price copied from the catalog.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the discount percentage the client asked for, or null to receive the ceiling.
    /// </summary>
    public decimal? RequestedDiscountPercentage { get; set; }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the id of the discount policy that priced the item.
    /// </summary>
    public Guid DiscountPolicyId { get; set; }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the highest discount percentage the policy allowed for the product's total when the item was priced.
    /// </summary>
    public decimal DiscountCeilingPercentage { get; set; }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the applied discount percentage: the requested one, or the ceiling when none was requested, never
    /// above the ceiling.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the discount amount: quantity times unit price times the applied percentage, rounded to cents.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the item total: quantity times unit price minus the discount amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
