namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;

// Work item: TASK-063 (FEAT-001), TD-032
/// <summary>
/// A discount policy with its tiers, returned by the create, get, list, and disable operations and, wrapped in the API
/// envelope, by the endpoints (A11).
/// </summary>
public class DiscountPolicyResult
{
    /// <summary>
    /// The policy id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The product the policy is limited to, or null for every product.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// The branch the policy is limited to, or null for every branch.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// The UTC instant the policy starts applying (inclusive).
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// The UTC instant the policy stops applying (exclusive), or null.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// The most units of one product a sale may carry.
    /// </summary>
    public int MaxQuantityPerProduct { get; set; }

    /// <summary>
    /// The UTC instant the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Work item: TD-032
    /// <summary>
    /// The UTC instant the policy was disabled, or null while it is active.
    /// </summary>
    public DateTime? DisabledAt { get; set; }

    /// <summary>
    /// The tiers, ordered by minimum quantity.
    /// </summary>
    public List<DiscountTierResult> Tiers { get; set; } = [];
}
