using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Profile for mapping the ListProducts request to the command and the result items to responses.
/// </summary>
public class ListProductsProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListProducts feature.
    /// </summary>
    public ListProductsProfile()
    {
        CreateMap<ListProductsRequest, ListProductsCommand>();
        CreateMap<ListProductsItem, ListProductsResponse>();
    }
}
