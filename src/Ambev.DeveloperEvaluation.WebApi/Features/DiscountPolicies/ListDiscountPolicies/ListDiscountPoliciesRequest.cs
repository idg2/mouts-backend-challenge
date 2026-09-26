namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Request model for listing discount policies, bound from the _page and _size query parameters.
/// </summary>
public class ListDiscountPoliciesRequest
{
    /// <summary>
    /// The page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;
}
