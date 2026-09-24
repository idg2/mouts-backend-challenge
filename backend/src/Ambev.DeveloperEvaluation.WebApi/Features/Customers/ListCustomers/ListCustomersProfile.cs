using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the ListCustomers request to the command and the result items to responses.
/// </summary>
public class ListCustomersProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListCustomers feature.
    /// </summary>
    public ListCustomersProfile()
    {
        CreateMap<ListCustomersRequest, ListCustomersCommand>();
        CreateMap<ListCustomersItem, ListCustomersResponse>();
    }
}
