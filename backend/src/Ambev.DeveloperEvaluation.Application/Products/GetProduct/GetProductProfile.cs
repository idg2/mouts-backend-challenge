using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Profile for mapping the Product entity to the GetProduct result.
/// </summary>
public class GetProductProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the GetProduct operation.
    /// </summary>
    public GetProductProfile()
    {
        CreateMap<Product, GetProductResult>();
    }
}
