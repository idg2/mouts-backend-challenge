namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// The fields a discount policy list can filter and order by, read by <c>ListQueryParser</c>. It is not a response: the
/// list answers with <c>DiscountPolicyResult</c>, whose tier collection cannot be a filter or an order field.
/// </summary>
public class DiscountPolicyListFields
{
    /// <summary>
    /// The policy id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The product scope; matches one id, never the null scope.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// The branch scope; matches one id, never the null scope.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// The UTC start; also accepts _minValidFrom and _maxValidFrom.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// The UTC creation instant; also accepts _minCreatedAt and _maxCreatedAt.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
