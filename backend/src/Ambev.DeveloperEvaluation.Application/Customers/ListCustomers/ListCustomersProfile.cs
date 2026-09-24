using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the Customer entity to the ListCustomers item.
/// </summary>
public class ListCustomersProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListCustomers operation.
    /// </summary>
    public ListCustomersProfile()
    {
        CreateMap<Customer, ListCustomersItem>();
    }
}
