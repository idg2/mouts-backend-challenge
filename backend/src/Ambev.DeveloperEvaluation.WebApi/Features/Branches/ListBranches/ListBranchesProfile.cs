using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the ListBranches request to the command and the result items to responses.
/// </summary>
public class ListBranchesProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the ListBranches feature.
    /// </summary>
    public ListBranchesProfile()
    {
        CreateMap<ListBranchesRequest, ListBranchesCommand>();
        CreateMap<ListBranchesItem, ListBranchesResponse>();
    }
}
