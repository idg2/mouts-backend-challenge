using Ambev.DeveloperEvaluation.Application.DiscountPolicies.GetDiscountPolicy;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.GetDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Profile for mapping the GetDiscountPolicy route id to the command.
/// </summary>
public class GetDiscountPolicyProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the GetDiscountPolicy feature.
    /// </summary>
    public GetDiscountPolicyProfile()
    {
        CreateMap<Guid, GetDiscountPolicyCommand>()
            .ConstructUsing(id => new GetDiscountPolicyCommand(id));
    }
}
