using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.GetDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Command for retrieving a discount policy by id.
/// </summary>
public record GetDiscountPolicyCommand : IRequest<DiscountPolicyResult>
{
    /// <summary>
    /// The id of the policy to retrieve.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of GetDiscountPolicyCommand.
    /// </summary>
    /// <param name="id">The id of the policy to retrieve</param>
    public GetDiscountPolicyCommand(Guid id)
    {
        Id = id;
    }
}
