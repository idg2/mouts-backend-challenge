using Ambev.DeveloperEvaluation.Application.Sales.Common;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the shared sale results to responses.
/// </summary>
public class SaleResponseProfile : Profile
{
    /// <summary>
    /// Initializes the mappings shared by the create, get, and update features.
    /// </summary>
    public SaleResponseProfile()
    {
        CreateMap<SaleResult, SaleResponse>();
        CreateMap<SaleItemResult, SaleItemResponse>();
    }
}
