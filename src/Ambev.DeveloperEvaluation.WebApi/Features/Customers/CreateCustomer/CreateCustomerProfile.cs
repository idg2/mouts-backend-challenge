using Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the CreateCustomer request to the command and the result to the response.
/// </summary>
public class CreateCustomerProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the CreateCustomer feature.
    /// </summary>
    public CreateCustomerProfile()
    {
        CreateMap<CreateCustomerRequest, CreateCustomerCommand>();
        CreateMap<CreateCustomerResult, CreateCustomerResponse>();
    }
}
