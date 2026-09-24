using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Profile for mapping between the CreateProduct command, the Product entity, and the result.
/// </summary>
public class CreateProductProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the CreateProduct operation.
    /// </summary>
    public CreateProductProfile()
    {
        CreateMap<CreateProductCommand, Product>();
        CreateMap<Product, CreateProductResult>();
    }
}
