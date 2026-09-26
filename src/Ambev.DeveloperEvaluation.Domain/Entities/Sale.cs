using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-015 (FEAT-010), TASK-062 (FEAT-001)
/// <summary>
/// Represents a sale record. Customer and branch are referenced by id with copies of their names
/// (External Identities); discounts and totals are computed from the discount policies by <see cref="ApplyDiscounts"/>.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>
    /// Gets or sets the sequential sale number assigned by the database.
    /// </summary>
    public long SaleNumber { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// Gets or sets the customer id (external identity, no foreign key).
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer name copied from the catalog.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch id (external identity, no foreign key).
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Gets or sets the branch name copied from the catalog.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Gets or sets the sale total: the sum of the totals of the active items, computed by <see cref="ApplyDiscounts"/>.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Gets or sets the sale items.
    /// </summary>
    public List<SaleItem> Items { get; set; } = [];

    /// <summary>
    /// Validates the sale against the <see cref="SaleValidator"/> rules.
    /// </summary>
    /// <returns>The validation result with any errors found.</returns>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    // Work item: TD-010 (FEAT-010), TASK-053 (FEAT-017), TASK-062 (FEAT-001)
    /// <summary>
    /// Replaces the item list with the incoming items, matching them by id.
    /// </summary>
    /// <remarks>
    /// An incoming item with <see cref="Guid.Empty"/> as id is added. An incoming item with an id updates the existing
    /// item with that id, which must exist: an active incoming item (including one that reactivates a cancelled item)
    /// copies its product, description, unit price, quantity, requested discount, and cancelled flag, and the discount
    /// snapshot and the totals are left to <see cref="ApplyDiscounts"/>; a cancelled incoming item copies only the
    /// cancelled flag, so the item keeps the values it was priced with. An existing item whose id is not among the
    /// incoming items is removed. Every resulting item is numbered by its position in the incoming list.
    /// </remarks>
    /// <param name="incoming">The complete list of items the sale must have.</param>
    public void SyncItems(IReadOnlyCollection<SaleItem> incoming)
    {
        var incomingIds = incoming
            .Where(item => item.Id != Guid.Empty)
            .Select(item => item.Id)
            .ToHashSet();

        var removed = Items.RemoveAll(existing => !incomingIds.Contains(existing.Id));

        var lineNumber = 0;
        foreach (var item in incoming)
        {
            lineNumber++;
            if (item.Id == Guid.Empty)
            {
                item.LineNumber = lineNumber;
                Items.Add(item);
                continue;
            }

            var target = Items.Single(existing => existing.Id == item.Id);
            CopyValues(item, target);
            target.LineNumber = lineNumber;
        }

        StepTrace.Step("SAL-UPD-10", "SyncItems removes, updates, adds, and renumbers", [("saleId", Id), ("removed", removed), ("updated", incomingIds.Count), ("added", incoming.Count - incomingIds.Count), ("items", Items.Count)]);
    }

    // Work item: TASK-062 (FEAT-001), TASK-066 (FEAT-001)
    /// <summary>
    /// Prices the items from the discount policies (spec section 3.5). The active items of each product are evaluated
    /// on their summed quantity; each gets the tier ceiling, or its requested discount when lower, a discount amount
    /// rounded to cents (midpoint away from zero), and a snapshot of the policy. A cancelled item keeps the values it
    /// was last priced with; one that was never priced gets its policy with no discount. The sale total is the sum of
    /// the active item totals.
    /// </summary>
    /// <param name="policiesByProduct">The policy of each product of the sale, from <c>DiscountPolicyResolver</c></param>
    /// <exception cref="DomainException">
    /// A product that needs pricing has no policy, or a product total is above its policy maximum or above
    /// <see cref="int.MaxValue"/> (the total is summed as <see cref="long"/>). The sale handlers check both before
    /// calling, so this is defense in depth.
    /// </exception>
    public void ApplyDiscounts(IReadOnlyDictionary<Guid, DiscountPolicy> policiesByProduct)
    {
        var activeItems = Items.Where(item => !item.IsCancelled).ToList();
        foreach (var group in activeItems.GroupBy(item => item.ProductId))
        {
            var policy = PolicyOf(group.Key, policiesByProduct);
            var total = group.Sum(item => (long)item.Quantity);
            var decision = policy.Evaluate((int)Math.Min(total, int.MaxValue));
            if (total > int.MaxValue || !decision.IsAllowed)
                throw new DomainException($"Total of {total} units for product {group.Key} exceeds maximum of {decision.MaxQuantity}");

            foreach (var item in group)
            {
                var applied = item.RequestedDiscountPercentage is decimal requested
                    ? Math.Min(requested, decision.CeilingPercentage)
                    : decision.CeilingPercentage;
                Price(item, decision.PolicyId, decision.CeilingPercentage, applied);
            }
        }

        foreach (var item in Items.Where(item => item.IsCancelled && item.DiscountPolicyId == Guid.Empty))
            Price(item, PolicyOf(item.ProductId, policiesByProduct).Id, 0m, 0m);

        TotalAmount = activeItems.Sum(item => item.TotalAmount);
    }

    // Work item: TASK-062 (FEAT-001)
    /// <summary>
    /// Copies an incoming item onto an existing one. A cancelled incoming item copies only the cancelled flag, since
    /// <see cref="ApplyDiscounts"/> leaves a priced cancelled item untouched and its values must stay consistent with
    /// its discount snapshot; an active incoming item copies every client-supplied value.
    /// </summary>
    private static void CopyValues(SaleItem source, SaleItem target)
    {
        target.IsCancelled = source.IsCancelled;
        if (source.IsCancelled)
            return;

        target.ProductId = source.ProductId;
        target.ProductDescription = source.ProductDescription;
        target.UnitPrice = source.UnitPrice;
        target.Quantity = source.Quantity;
        target.RequestedDiscountPercentage = source.RequestedDiscountPercentage;
    }

    // Work item: TASK-062 (FEAT-001)
    private DiscountPolicy PolicyOf(Guid productId, IReadOnlyDictionary<Guid, DiscountPolicy> policiesByProduct) =>
        policiesByProduct.TryGetValue(productId, out var policy)
            ? policy
            : throw new DomainException(
                $"No discount policy in effect for product {productId} at branch {BranchId} on {SaleDate.ToString("O", CultureInfo.InvariantCulture)}");

    // Work item: TASK-062 (FEAT-001)
    private static void Price(SaleItem item, Guid policyId, decimal ceilingPercentage, decimal appliedPercentage)
    {
        var gross = item.Quantity * item.UnitPrice;
        item.DiscountPolicyId = policyId;
        item.DiscountCeilingPercentage = ceilingPercentage;
        item.DiscountPercentage = appliedPercentage;
        item.DiscountAmount = Math.Round(gross * appliedPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        item.TotalAmount = gross - item.DiscountAmount;
    }
}
