using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-015 (FEAT-010), TASK-062 (FEAT-001), TD-039
/// <summary>
/// Represents one line of a sale. The product is referenced by id; its description and unit price
/// are copies taken when the line was created or its product changed. The discount fields are a snapshot of the
/// discount policy that priced the line (<see cref="Sale.ApplyDiscounts"/>).
/// </summary>
public class SaleItem : BaseEntity
{
    // Work item: TD-039
    /// <summary>
    /// Gets the id of the sale this item belongs to.
    /// </summary>
    public Guid SaleId { get; private set; }

    // Work item: TD-010 (FEAT-010), TD-039
    /// <summary>
    /// Gets the position of the item in the sale, starting at 1, in the order the client sent the items.
    /// </summary>
    public int LineNumber { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the product id (external identity, no foreign key).
    /// </summary>
    public Guid ProductId { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the product description copied from the catalog.
    /// </summary>
    public string ProductDescription { get; private set; } = string.Empty;

    // Work item: TD-039
    /// <summary>
    /// Gets the unit price copied from the catalog.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the quantity.
    /// </summary>
    public int Quantity { get; private set; }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the discount percentage the client asked for, or null to receive the ceiling.
    /// </summary>
    public decimal? RequestedDiscountPercentage { get; private set; }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the id of the discount policy that priced the item.
    /// </summary>
    public Guid DiscountPolicyId { get; private set; }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the highest discount percentage the policy allowed for the product's total when the item was priced.
    /// </summary>
    public decimal DiscountCeilingPercentage { get; private set; }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the applied discount percentage: the requested one, or the ceiling when none was requested, never
    /// above the ceiling.
    /// </summary>
    public decimal DiscountPercentage { get; private set; }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the discount amount: quantity times unit price times the applied percentage, rounded to cents.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the item total: quantity times unit price minus the discount amount.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets whether the item is cancelled.
    /// </summary>
    public bool IsCancelled { get; private set; }

    // Work item: TD-039
    private SaleItem()
    {
    }

    // Work item: TD-039
    /// <summary>
    /// Creates a new item from a line. The id is left empty for the column default.
    /// </summary>
    /// <param name="line">The line</param>
    /// <param name="lineNumber">The position of the item in the sale, from 1</param>
    /// <returns>The item, not priced yet</returns>
    internal static SaleItem From(SaleLine line, int lineNumber) => new()
    {
        LineNumber = lineNumber,
        ProductId = line.ProductId,
        ProductDescription = line.ProductDescription,
        UnitPrice = line.UnitPrice,
        Quantity = line.Quantity,
        RequestedDiscountPercentage = line.RequestedDiscountPercentage,
        IsCancelled = line.IsCancelled
    };

    // Work item: TD-039
    /// <summary>
    /// Copies a line onto this item. A cancelled line copies only the cancelled flag, since
    /// <see cref="Sale.ApplyDiscounts"/> leaves a priced cancelled item untouched and its values must stay consistent
    /// with its discount snapshot; an active line copies every client-supplied value.
    /// </summary>
    /// <param name="line">The line</param>
    internal void Update(SaleLine line)
    {
        IsCancelled = line.IsCancelled;
        if (line.IsCancelled)
            return;

        ProductId = line.ProductId;
        ProductDescription = line.ProductDescription;
        UnitPrice = line.UnitPrice;
        Quantity = line.Quantity;
        RequestedDiscountPercentage = line.RequestedDiscountPercentage;
    }

    // Work item: TD-039
    /// <summary>
    /// Moves the item to a new position in the sale.
    /// </summary>
    /// <param name="lineNumber">The position, from 1</param>
    internal void Renumber(int lineNumber) => LineNumber = lineNumber;

    // Work item: TD-039
    /// <summary>
    /// Records the discount snapshot and computes the discount amount, rounded to cents (midpoint away from zero), and
    /// the item total.
    /// </summary>
    /// <param name="policyId">The policy that priced the item</param>
    /// <param name="ceilingPercentage">The ceiling the policy allowed for the product's total</param>
    /// <param name="appliedPercentage">The applied percentage</param>
    internal void Price(Guid policyId, decimal ceilingPercentage, decimal appliedPercentage)
    {
        var gross = Quantity * UnitPrice;
        DiscountPolicyId = policyId;
        DiscountCeilingPercentage = ceilingPercentage;
        DiscountPercentage = appliedPercentage;
        DiscountAmount = Math.Round(gross * appliedPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        TotalAmount = gross - DiscountAmount;
    }
}
