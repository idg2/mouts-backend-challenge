using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the Customer entity to the GetCustomer result.
/// </summary>
public class GetCustomerProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the GetCustomer operation.
    /// </summary>
    public GetCustomerProfile()
    {
        CreateMap<Customer, GetCustomerResult>();
    }
}
