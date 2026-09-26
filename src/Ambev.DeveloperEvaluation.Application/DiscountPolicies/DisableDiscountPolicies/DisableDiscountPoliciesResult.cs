using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Response model for the DisableDiscountPolicies operation.
/// </summary>
public class DisableDiscountPoliciesResult
{
    /// <summary>
    /// The disabled policies, with their tiers and DisabledAt, in the order of the requested ids.
    /// </summary>
    public List<DiscountPolicyResult> Policies { get; set; } = [];
}
