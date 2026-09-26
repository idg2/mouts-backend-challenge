using Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.DisableDiscountPolicies;

// Work item: TD-032
/// <summary>
/// Profile for mapping the DisableDiscountPolicies request to the command. The response is the Application result (A11).
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
