using Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.DeleteCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Profile for mapping the DeleteCustomer route id to the command.
/// </summary>
public class DeleteCustomerProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the DeleteCustomer feature.
    /// </summary>
    public DeleteCustomerProfile()
    {
        CreateMap<Guid, DeleteCustomerCommand>()
            .ConstructUsing(id => new DeleteCustomerCommand(id));
    }
}
