using Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the UpdateCustomer request to the command and the result to the response.
/// </summary>
public class UpdateCustomerProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateCustomer feature.
    /// </summary>
    public UpdateCustomerProfile()
    {
        CreateMap<UpdateCustomerRequest, UpdateCustomerCommand>();
        CreateMap<UpdateCustomerResult, UpdateCustomerResponse>();
    }
}
