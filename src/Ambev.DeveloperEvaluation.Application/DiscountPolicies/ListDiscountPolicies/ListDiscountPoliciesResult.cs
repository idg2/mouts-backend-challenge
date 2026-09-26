using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Response model for the ListDiscountPolicies operation: one page of policies, each with its tiers, and the total count.
/// </summary>
public class ListDiscountPoliciesResult
{
    /// <summary>
    /// The policies on the requested page.
    /// </summary>
    public List<DiscountPolicyResult> Items { get; set; } = [];

    /// <summary>
    /// The number of policies that match the filters.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    public int Size { get; set; }
}
