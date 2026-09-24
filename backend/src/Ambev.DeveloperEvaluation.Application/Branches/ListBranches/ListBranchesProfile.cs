using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the Branch entity to the ListBranches item.
/// </summary>
public class ListBranchesProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListBranches operation.
    /// </summary>
    public ListBranchesProfile()
    {
        CreateMap<Branch, ListBranchesItem>();
    }
}
