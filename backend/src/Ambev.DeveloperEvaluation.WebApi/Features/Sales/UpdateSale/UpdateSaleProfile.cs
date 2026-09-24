using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Profile for mapping the UpdateSale request to the command.
/// </summary>
public class UpdateSaleProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateSale feature.
    /// </summary>
    public UpdateSaleProfile()
    {
        CreateMap<UpdateSaleRequest, UpdateSaleCommand>();
        CreateMap<UpdateSaleItemRequest, UpdateSaleItemInput>();
    }
}
