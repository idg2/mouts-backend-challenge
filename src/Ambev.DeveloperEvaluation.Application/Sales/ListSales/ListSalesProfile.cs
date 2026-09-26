using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the Sale entity and the sale snapshot of the read model to the ListSales item.
/// </summary>
public class ListSalesProfile : Profile
{
    // Work item: TASK-077 (FEAT-003)
    /// <summary>
    /// Initializes the mappings for the ListSales operation.
    /// </summary>
    public ListSalesProfile()
    {
        CreateMap<Sale, ListSalesItem>();
        CreateMap<SaleSnapshot, ListSalesItem>()
            .ForMember(item => item.Id, opt => opt.MapFrom(snapshot => snapshot.SaleId));
    }
}
