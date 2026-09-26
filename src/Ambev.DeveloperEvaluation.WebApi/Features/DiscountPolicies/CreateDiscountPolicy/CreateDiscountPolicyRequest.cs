namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Represents a request to create a discount policy. Dates are UTC and end in Z.
/// </summary>
public class CreateDiscountPolicyRequest
{
    /// <summary>
    /// Gets or sets the product scope, or null for every product.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the branch scope, or null for every branch.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Gets or sets the UTC start (inclusive); not before the current time.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Gets or sets the UTC end (exclusive), or null for no end.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Gets or sets the most units of one product a sale may carry; above zero.
    /// </summary>
    public int MaxQuantityPerProduct { get; set; }

    /// <summary>
    /// Gets or sets the tiers, sorted by minimum quantity, not overlapping, within the maximum; empty means no discount.
    /// </summary>
    public List<DiscountTierRequest> Tiers { get; set; } = [];
}
