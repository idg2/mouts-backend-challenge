namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.GetDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Request model for getting a discount policy by id.
/// </summary>
public class GetDiscountPolicyRequest
{
    /// <summary>
    /// The id of the policy to retrieve.
    /// </summary>
    public Guid Id { get; set; }
}
