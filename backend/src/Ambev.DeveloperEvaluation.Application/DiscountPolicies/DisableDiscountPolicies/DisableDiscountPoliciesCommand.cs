using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Command for disabling discount policies, all or nothing: an unknown id disables none of them.
/// </summary>
public class DisableDiscountPoliciesCommand : IRequest<DisableDiscountPoliciesResult>, ITransactionalCommand
{
    /// <summary>
    /// Gets or sets the ids of the policies to disable; at least one, none empty, none repeated.
    /// </summary>
    public List<Guid> Ids { get; set; } = [];
}
