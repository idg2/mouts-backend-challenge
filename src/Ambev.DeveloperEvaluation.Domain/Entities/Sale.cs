using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-015 (FEAT-010), TASK-062 (FEAT-001), TD-039
/// <summary>
/// Represents a sale record (aggregate root). Customer and branch are referenced by id with copies of their names
/// (External Identities); items are created and changed only through the aggregate, and discounts and totals are
/// computed from the discount policies by <see cref="ApplyDiscounts"/>.
/// </summary>
public class Sale : BaseEntity
{
    // Work item: TD-039
    private readonly List<SaleItem> _items = [];

    // Work item: TD-039
    /// <summary>
    /// Gets the sequential sale number assigned by the database.
    /// </summary>
    public long SaleNumber { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the UTC date and time when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the customer id (external identity, no foreign key).
    /// </summary>
    public Guid CustomerId { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the customer name copied from the catalog.
    /// </summary>
    public string CustomerName { get; private set; } = string.Empty;

    // Work item: TD-039
    /// <summary>
    /// Gets the branch id (external identity, no foreign key).
    /// </summary>
    public Guid BranchId { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the branch name copied from the catalog.
    /// </summary>
    public string BranchName { get; private set; } = string.Empty;

    // Work item: TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Gets the sale total: the sum of the totals of the active items, computed by <see cref="ApplyDiscounts"/>.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; private set; }

    // Work item: TD-039
    /// <summary>
    /// Gets the sale items. Only the aggregate adds, changes, or removes them.
    /// </summary>
    public IReadOnlyList<SaleItem> Items => _items;

    // Work item: TD-039
    private Sale()
    {
    }

    // Work item: TD-039
    /// <summary>
    /// Creates an active sale with one item per line, numbered from 1 in the order given. The discounts and the total
    /// are left to <see cref="ApplyDiscounts"/>.
    /// </summary>
    /// <param name="id">The sale id, or <see cref="Guid.Empty"/> to leave it to the column default</param>
    /// <param name="saleDate">The UTC sale date</param>
    /// <param name="customerId">The customer id (external identity)</param>
    /// <param name="customerName">The customer name copy</param>
    /// <param name="branchId">The branch id (external identity)</param>
    /// <param name="branchName">The branch name copy</param>
    /// <param name="lines">The items, each with a null item id</param>
    /// <returns>The sale</returns>
    public static Sale Create(
        Guid id, DateTime saleDate, Guid customerId, string customerName, Guid branchId, string branchName,
        IReadOnlyList<SaleLine> lines)
    {
        var sale = new Sale
        {
            Id = id,
            SaleDate = saleDate,
            CustomerId = customerId,
            CustomerName = customerName,
            BranchId = branchId,
            BranchName = branchName
        };
        for (var index = 0; index < lines.Count; index++)
            sale._items.Add(SaleItem.From(lines[index], index + 1));
        return sale;
    }

    // Work item: TD-039
    /// <summary>
    /// Points the sale at another customer.
    /// </summary>
    /// <param name="customerId">The customer id (external identity)</param>
    /// <param name="customerName">The customer name copy</param>
    public void ChangeCustomer(Guid customerId, string customerName)
    {
        CustomerId = customerId;
        CustomerName = customerName;
    }

    // Work item: TD-039
    /// <summary>
    /// Points the sale at another branch.
    /// </summary>
    /// <param name="branchId">The branch id (external identity)</param>
    /// <param name="branchName">The branch name copy</param>
    public void ChangeBranch(Guid branchId, string branchName)
    {
        BranchId = branchId;
        BranchName = branchName;
    }

    // Work item: TD-039
    /// <summary>
    /// Sets the cancelled flag. Both directions are allowed; the sale state rules are FEAT-002.
    /// </summary>
    /// <param name="isCancelled">Whether the sale is cancelled</param>
    public void SetCancelled(bool isCancelled) => IsCancelled = isCancelled;

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

    // Work item: TD-010 (FEAT-010), TASK-053 (FEAT-017), TASK-062 (FEAT-001), TD-039
    /// <summary>
    /// Replaces the item list with the incoming lines, matching them to items by id.
    /// </summary>
    /// <remarks>
    /// A line without item id is added as a new item. A line with an item id updates the existing item with that id,
    /// which must exist: an active line (including one that reactivates a cancelled item) copies its product,
    /// description, unit price, quantity, requested discount, and cancelled flag, and the discount snapshot and the
    /// totals are left to <see cref="ApplyDiscounts"/>; a cancelled line copies only the cancelled flag, so the item
    /// keeps the values it was priced with. An existing item whose id is not among the lines is removed. Every
    /// resulting item is numbered by its position in the lines.
    /// </remarks>
    /// <param name="incoming">The complete list of lines the sale must have.</param>
    public void SyncItems(IReadOnlyList<SaleLine> incoming)
    {
        var incomingIds = incoming
            .Where(line => line.ItemId.HasValue)
            .Select(line => line.ItemId!.Value)
            .ToHashSet();

        var removed = _items.RemoveAll(existing => !incomingIds.Contains(existing.Id));

        var lineNumber = 0;
        foreach (var line in incoming)
        {
            lineNumber++;
            if (line.ItemId is not Guid itemId)
            {
                _items.Add(SaleItem.From(line, lineNumber));
                continue;
            }

            var target = _items.Single(existing => existing.Id == itemId);
            target.Update(line);
            target.Renumber(lineNumber);
        }

        StepTrace.Step("SAL-UPD-10", "SyncItems removes, updates, adds, and renumbers", [("saleId", Id), ("removed", removed), ("updated", incomingIds.Count), ("added", incoming.Count - incomingIds.Count), ("items", _items.Count)]);
    }

    // Work item: TASK-062 (FEAT-001), TASK-066 (FEAT-001), TD-039
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
                item.Price(decision.PolicyId, decision.CeilingPercentage, applied);
            }
        }

        foreach (var item in Items.Where(item => item.IsCancelled && item.DiscountPolicyId == Guid.Empty))
            item.Price(PolicyOf(item.ProductId, policiesByProduct).Id, 0m, 0m);

        TotalAmount = activeItems.Sum(item => item.TotalAmount);
    }

    // Work item: TASK-062 (FEAT-001)
    private DiscountPolicy PolicyOf(Guid productId, IReadOnlyDictionary<Guid, DiscountPolicy> policiesByProduct) =>
        policiesByProduct.TryGetValue(productId, out var policy)
            ? policy
            : throw new DomainException(
                $"No discount policy in effect for product {productId} at branch {BranchId} on {SaleDate.ToString("O", CultureInfo.InvariantCulture)}");
}
