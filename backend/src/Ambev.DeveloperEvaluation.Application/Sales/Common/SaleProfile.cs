using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Profile for mapping the Sale and SaleItem entities to their results.
/// </summary>
public class SaleProfile : Profile
{
    /// <summary>
    /// Initializes the mappings shared by the create, get, and update operations.
    /// </summary>
    public SaleProfile()
    {
        CreateMap<Sale, SaleResult>();
        CreateMap<SaleItem, SaleItemResult>();
    }
}
