namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// One tier of a <see cref="CreateDiscountPolicy.CreateDiscountPolicyCommand"/>.
/// </summary>
public class DiscountTierInput
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
    /// Gets or sets the discount ceiling in percent, above 0 and at most 100.
    /// </summary>
    public decimal Percentage { get; set; }
}
