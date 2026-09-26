using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

// Work item: TASK-060 (FEAT-001), TD-032
/// <summary>
/// A discount policy: for a scope (a product, a branch, both, or neither for the default), from <see cref="ValidFrom"/>
/// until <see cref="ValidTo"/> (exclusive), at most <see cref="MaxQuantityPerProduct"/> units of one product per sale,
/// with a discount ceiling per quantity tier. Policies are never edited: a new rule is a new policy that wins by scope,
/// then by later start, then by later creation, and the only change after creation is <see cref="Disable"/>.
/// </summary>
public class DiscountPolicy : BaseEntity
{
    private readonly List<DiscountTier> _tiers = [];

    /// <summary>
    /// Gets the product the policy is limited to (external identity), or null for every product.
    /// </summary>
    public Guid? ProductId { get; private set; }

    /// <summary>
    /// Gets the branch the policy is limited to (external identity), or null for every branch.
    /// </summary>
    public Guid? BranchId { get; private set; }

    /// <summary>
    /// Gets the UTC instant the policy starts applying (inclusive).
    /// </summary>
    public DateTime ValidFrom { get; private set; }

    /// <summary>
    /// Gets the UTC instant the policy stops applying (exclusive), or null for no end.
    /// </summary>
    public DateTime? ValidTo { get; private set; }

    /// <summary>
    /// Gets the most units of one product a sale may carry under this policy.
    /// </summary>
    public int MaxQuantityPerProduct { get; private set; }

    /// <summary>
    /// Gets the UTC instant the policy was created; the last tie-break between policies of one scope and one start.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Work item: TD-032
    /// <summary>
    /// Gets the UTC instant the policy was disabled, or null while it is active. A disabled policy never applies again.
    /// </summary>
    public DateTime? DisabledAt { get; private set; }

    /// <summary>
    /// Gets the tiers, ordered by minimum quantity when created.
    /// </summary>
    public IReadOnlyList<DiscountTier> Tiers => _tiers;

    /// <summary>
    /// Gets the scope precedence: 3 for product and branch, 2 for product, 1 for branch, 0 for the default.
    /// </summary>
    public int SpecificityRank => (ProductId is null ? 0 : 2) + (BranchId is null ? 0 : 1);

    // Used by EF Core.
    private DiscountPolicy()
    {
    }

    /// <summary>
    /// Creates a policy after checking every invariant.
    /// </summary>
    /// <param name="productId">The product scope, or null</param>
    /// <param name="branchId">The branch scope, or null</param>
    /// <param name="validFrom">The UTC start; not before <paramref name="now"/></param>
    /// <param name="validTo">The UTC end, later than the start, or null</param>
    /// <param name="maxQuantityPerProduct">The most units of one product per sale, above zero</param>
    /// <param name="tiers">The tiers, sorted by minimum, not overlapping, within the maximum</param>
    /// <param name="now">The current UTC instant, stored as <see cref="CreatedAt"/></param>
    /// <returns>The new policy with a new id</returns>
    /// <exception cref="DomainException">An invariant is broken</exception>
    public static DiscountPolicy Create(
        Guid? productId,
        Guid? branchId,
        DateTime validFrom,
        DateTime? validTo,
        int maxQuantityPerProduct,
        IEnumerable<DiscountTier> tiers,
        DateTime now)
    {
        if (productId == Guid.Empty || branchId == Guid.Empty)
            throw new DomainException("Scope identifiers must be null or a non-empty id");
        EnsureUtc(validFrom, nameof(ValidFrom));
        if (validTo is not null)
            EnsureUtc(validTo.Value, nameof(ValidTo));
        if (validFrom < now)
            throw new DomainException($"ValidFrom {validFrom:O} is in the past; policies cannot be retroactive");
        if (validTo is not null && validTo <= validFrom)
            throw new DomainException("ValidTo must be later than ValidFrom");
        if (maxQuantityPerProduct <= 0)
            throw new DomainException("Maximum quantity per product must be greater than zero");

        var tierList = tiers.ToList();
        var violation = DiscountTier.FindSetViolation(tierList.Select(tier => (tier.MinQuantity, tier.MaxQuantity)), maxQuantityPerProduct);
        if (violation is not null)
            throw new DomainException(violation);

        var policy = new DiscountPolicy
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            BranchId = branchId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            MaxQuantityPerProduct = maxQuantityPerProduct,
            CreatedAt = now
        };
        policy._tiers.AddRange(tierList);
        return policy;
    }

    // Work item: TD-032
    /// <summary>
    /// Disables the policy: from now on it takes no part in resolving a sale's discounts. Disabling a disabled policy
    /// keeps the first instant. It cannot be undone.
    /// </summary>
    /// <param name="now">The current UTC instant, stored as <see cref="DisabledAt"/></param>
    /// <exception cref="DomainException"><paramref name="now"/> is not UTC</exception>
    public void Disable(DateTime now)
    {
        EnsureUtc(now, nameof(DisabledAt));
        if (DisabledAt is not null)
            return;

        DisabledAt = now;
    }

    // Work item: TASK-060 (FEAT-001), TD-032, TD-043
    /// <summary>
    /// Returns whether the policy applies at an instant: <c>ValidFrom &lt;= date &lt; ValidTo</c> and the policy was not
    /// yet disabled at that instant. A disable ends the policy from then on and leaves the sales dated before it priced
    /// by it, so they stay editable.
    /// </summary>
    /// <param name="date">The UTC instant, usually a sale date</param>
    /// <returns>True when the policy is in effect</returns>
    public bool IsInEffectAt(DateTime date) =>
        (DisabledAt is null || date < DisabledAt) && ValidFrom <= date && (ValidTo is null || date < ValidTo);

    /// <summary>
    /// Evaluates the total quantity of one product in a sale.
    /// </summary>
    /// <param name="totalQuantity">The sum of the quantities of the product's active lines, at least 1</param>
    /// <returns>Whether the total is allowed and the ceiling of the tier it falls in</returns>
    /// <exception cref="DomainException">The total is below 1</exception>
    public DiscountDecision Evaluate(int totalQuantity)
    {
        if (totalQuantity < 1)
            throw new DomainException("Quantity must be at least 1");

        if (totalQuantity > MaxQuantityPerProduct)
            return new DiscountDecision(false, 0m, Id, MaxQuantityPerProduct);

        var ceiling = _tiers.FirstOrDefault(tier => tier.Contains(totalQuantity))?.Percentage ?? 0m;
        return new DiscountDecision(true, ceiling, Id, MaxQuantityPerProduct);
    }

    private static void EnsureUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new DomainException($"{name} must be in UTC");
    }
}
