using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products.UpdateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Profile for mapping the UpdateProduct request to the command and the result to the response.
/// </summary>
public class UpdateProductProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateProduct feature.
    /// </summary>
    public UpdateProductProfile()
    {
        CreateMap<UpdateProductRequest, UpdateProductCommand>();
        CreateMap<UpdateProductResult, UpdateProductResponse>();
    }
}
