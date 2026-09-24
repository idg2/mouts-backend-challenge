using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Profile for mapping the Product entity to the UpdateProduct result.
/// </summary>
public class UpdateProductProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateProduct operation.
    /// </summary>
    public UpdateProductProfile()
    {
        CreateMap<Product, UpdateProductResult>();
    }
}
