using Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032, TD-043
/// <summary>
/// Profile for mapping the DisableDiscountPolicies request to the command. The endpoint
/// returns the Application result itself, so no response is mapped.
/// </summary>
public class DisableDiscountPoliciesProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the DisableDiscountPolicies feature.
    /// </summary>
    public DisableDiscountPoliciesProfile()
    {
        CreateMap<DisableDiscountPoliciesRequest, DisableDiscountPoliciesCommand>();
    }
}
