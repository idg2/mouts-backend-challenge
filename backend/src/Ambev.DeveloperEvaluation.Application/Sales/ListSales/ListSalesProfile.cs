using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the Sale entity to the ListSales item.
/// </summary>
public class ListSalesProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListSales operation.
    /// </summary>
    public ListSalesProfile()
    {
        CreateMap<Sale, ListSalesItem>();
    }
}
