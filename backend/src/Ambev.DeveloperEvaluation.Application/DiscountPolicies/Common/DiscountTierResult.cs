namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// One tier of a <see cref="DiscountPolicyResult"/>.
/// </summary>
public class DiscountTierResult
{
    /// <summary>
    /// The lowest product total the tier covers.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// The highest product total the tier covers, or null for no upper bound.
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// The discount ceiling in percent.
    /// </summary>
    public decimal Percentage { get; set; }
}
