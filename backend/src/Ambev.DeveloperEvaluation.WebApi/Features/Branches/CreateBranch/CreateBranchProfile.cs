using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the CreateBranch request to the command and the result to the response.
/// </summary>
public class CreateBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the CreateBranch feature.
    /// </summary>
    public CreateBranchProfile()
    {
        CreateMap<CreateBranchRequest, CreateBranchCommand>();
        CreateMap<CreateBranchResult, CreateBranchResponse>();
    }
}
