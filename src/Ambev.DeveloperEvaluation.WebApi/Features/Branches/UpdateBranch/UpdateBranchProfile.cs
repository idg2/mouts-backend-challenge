using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the UpdateBranch request to the command and the result to the response.
/// </summary>
public class UpdateBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the UpdateBranch feature.
    /// </summary>
    public UpdateBranchProfile()
    {
        CreateMap<UpdateBranchRequest, UpdateBranchCommand>();
        CreateMap<UpdateBranchResult, UpdateBranchResponse>();
    }
}
