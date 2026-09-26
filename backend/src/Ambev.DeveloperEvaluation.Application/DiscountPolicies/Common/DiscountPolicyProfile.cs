using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Profile for mapping the DiscountPolicy aggregate to its result, shared by the create, get, and list operations.
/// </summary>
public class DiscountPolicyProfile : Profile
{
    /// <summary>
    /// Initializes the mappings. Tiers are listed by minimum quantity, whatever order the database returned them in.
    /// </summary>
    public DiscountPolicyProfile()
    {
        CreateMap<DiscountTier, DiscountTierResult>();
        CreateMap<DiscountPolicy, DiscountPolicyResult>()
            .ForMember(result => result.Tiers, options => options.MapFrom(policy => policy.Tiers.OrderBy(tier => tier.MinQuantity)));
    }
}
