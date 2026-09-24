using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the GetBranch route id to the command and the result to the response.
/// </summary>
public class GetBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the GetBranch feature.
    /// </summary>
    public GetBranchProfile()
    {
        CreateMap<Guid, GetBranchCommand>()
            .ConstructUsing(id => new GetBranchCommand(id));
        CreateMap<GetBranchResult, GetBranchResponse>();
    }
}
