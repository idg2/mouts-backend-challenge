using Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.GetCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the GetCustomer route id to the command and the result to the response.
/// </summary>
public class GetCustomerProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the GetCustomer feature.
    /// </summary>
    public GetCustomerProfile()
    {
        CreateMap<Guid, GetCustomerCommand>()
            .ConstructUsing(id => new GetCustomerCommand(id));
        CreateMap<GetCustomerResult, GetCustomerResponse>();
    }
}
