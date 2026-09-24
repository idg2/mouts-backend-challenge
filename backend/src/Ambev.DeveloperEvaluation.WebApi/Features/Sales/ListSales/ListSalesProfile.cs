using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the ListSales request to the command and the result items to responses.
/// </summary>
public class ListSalesProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListSales feature.
    /// </summary>
    public ListSalesProfile()
    {
        CreateMap<ListSalesRequest, ListSalesCommand>();
        CreateMap<ListSalesItem, ListSalesResponse>();
    }
}
