using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the Branch entity to the GetBranch result.
/// </summary>
public class GetBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the GetBranch operation.
    /// </summary>
    public GetBranchProfile()
    {
        CreateMap<Branch, GetBranchResult>();
    }
}
