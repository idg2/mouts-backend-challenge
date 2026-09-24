using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the Customer entity to the UpdateCustomer result.
/// </summary>
public class UpdateCustomerProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateCustomer operation.
    /// </summary>
    public UpdateCustomerProfile()
    {
        CreateMap<Customer, UpdateCustomerResult>();
    }
}
