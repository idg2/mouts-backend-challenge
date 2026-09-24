using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Profile for mapping the Product entity to the ListProducts item.
/// </summary>
public class ListProductsProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListProducts operation.
    /// </summary>
    public ListProductsProfile()
    {
        CreateMap<Product, ListProductsItem>();
    }
}
