using Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Profile for mapping the ListDiscountPolicies request to the command.
/// </summary>
public class ListDiscountPoliciesProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListDiscountPolicies feature.
    /// </summary>
    public ListDiscountPoliciesProfile()
    {
        CreateMap<ListDiscountPoliciesRequest, ListDiscountPoliciesCommand>();
    }
}
