namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Request model for disabling discount policies.
/// </summary>
public class DisableDiscountPoliciesRequest
{
    /// <summary>
    /// Gets or sets the ids of the policies to disable; at least one, none empty, none repeated.
    /// </summary>
    public List<Guid> Ids { get; set; } = [];
}
