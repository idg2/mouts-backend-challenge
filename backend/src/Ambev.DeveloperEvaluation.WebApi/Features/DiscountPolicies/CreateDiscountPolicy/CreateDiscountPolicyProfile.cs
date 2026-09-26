using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Profile for mapping the CreateDiscountPolicy request to the command. The response is the Application result (A11).
/// </summary>
public class CreateDiscountPolicyProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the CreateDiscountPolicy feature.
    /// </summary>
    public CreateDiscountPolicyProfile()
    {
        CreateMap<CreateDiscountPolicyRequest, CreateDiscountPolicyCommand>();
        CreateMap<DiscountTierRequest, DiscountTierInput>();
    }
}
