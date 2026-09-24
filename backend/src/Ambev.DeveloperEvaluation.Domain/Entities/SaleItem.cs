using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-015 (FEAT-010)
/// <summary>
/// Represents one line of a sale. The product is referenced by id; its description and unit price
/// are copies taken when the line was created or its product changed.
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

    /// <summary>
    /// Gets or sets the discount percentage, stored as received.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the discount amount, stored as received.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the item total, stored as received.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
