using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the CreateSale request to the command.
/// </summary>
public class CreateSaleProfile : Profile
{
    // Work item: TD-012
    /// <summary>
    /// Initializes the mappings for the CreateSale feature. The command id is ignored: the server assigns it (the
    /// queued mode sets it before sending), so no request field may fill it.
    /// </summary>
    public CreateSaleProfile()
    {
        CreateMap<CreateSaleRequest, CreateSaleCommand>()
            .ForMember(command => command.Id, options => options.Ignore());
        CreateMap<CreateSaleItemRequest, CreateSaleItemInput>();
    }
}
