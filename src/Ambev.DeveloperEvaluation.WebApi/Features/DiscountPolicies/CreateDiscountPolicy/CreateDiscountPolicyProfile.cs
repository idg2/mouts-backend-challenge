using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;

// Work item: TASK-063 (FEAT-001), TD-043
/// <summary>
/// Profile for mapping the CreateDiscountPolicy request to the command. The endpoint
/// returns the Application result itself, so no response is mapped.
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
