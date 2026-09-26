namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// One tier of a <see cref="CreateDiscountPolicyRequest"/>.
/// </summary>
public class DiscountTierRequest
{
    /// <summary>
    /// Gets or sets the lowest product total the tier covers, at least 1.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// Gets or sets the highest product total the tier covers, or null for no upper bound.
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// Gets or sets the discount ceiling in percent, above 0 and at most 100, with at most two decimals.
    /// </summary>
    public decimal Percentage { get; set; }
}
