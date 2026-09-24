using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the Sale and SaleItem entities to their results.
/// </summary>
public class SaleProfile : Profile
{
    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Initializes the mappings shared by the create, get, and update operations. Items are listed by line
    /// number, the order the client sent them in, so every response shows the same order.
    /// </summary>
    public SaleProfile()
    {
        CreateMap<Sale, SaleResult>()
            .ForMember(result => result.Items, opt => opt.MapFrom(sale => sale.Items.OrderBy(item => item.LineNumber)));
        CreateMap<SaleItem, SaleItemResult>();
    }
}
