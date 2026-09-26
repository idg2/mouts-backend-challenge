using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the Branch entity to the UpdateBranch result.
/// </summary>
public class UpdateBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateBranch operation.
    /// </summary>
    public UpdateBranchProfile()
    {
        CreateMap<Branch, UpdateBranchResult>();
    }
}
