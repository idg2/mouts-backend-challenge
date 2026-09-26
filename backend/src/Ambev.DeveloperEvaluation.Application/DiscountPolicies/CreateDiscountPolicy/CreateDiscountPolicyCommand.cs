using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001), TD-032
/// <summary>
/// Command for creating a discount policy. Policies are never edited or deleted; the only later change is disabling.
/// </summary>
public class CreateDiscountPolicyCommand : IRequest<DiscountPolicyResult>, ITransactionalCommand
{
    /// <summary>
    /// Gets or sets the product scope, or null for every product.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the branch scope, or null for every branch.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Gets or sets the UTC start (inclusive); not before the current time.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Gets or sets the UTC end (exclusive), or null for no end.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Gets or sets the most units of one product a sale may carry.
    /// </summary>
    public int MaxQuantityPerProduct { get; set; }

    /// <summary>
    /// Gets or sets the tiers, sorted by minimum quantity.
    /// </summary>
    public List<DiscountTierInput> Tiers { get; set; } = [];
}
