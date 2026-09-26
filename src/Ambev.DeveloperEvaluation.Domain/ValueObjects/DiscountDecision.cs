namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

// Work item: TASK-060 (FEAT-001)
/// <summary>
/// The outcome of evaluating a product total against a discount policy.
/// </summary>
/// <param name="IsAllowed">True when the total is at most the policy maximum</param>
/// <param name="CeilingPercentage">The percentage of the tier the total falls in, or 0 below the first tier, in a gap, or above the maximum</param>
/// <param name="PolicyId">The policy that decided</param>
/// <param name="MaxQuantity">The policy maximum per product</param>
public sealed record DiscountDecision(bool IsAllowed, decimal CeilingPercentage, Guid PolicyId, int MaxQuantity);
