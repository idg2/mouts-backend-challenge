using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.DeleteBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping the DeleteBranch route id to the command.
/// </summary>
public class DeleteBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the DeleteBranch feature.
    /// </summary>
    public DeleteBranchProfile()
    {
        CreateMap<Guid, DeleteBranchCommand>()
            .ConstructUsing(id => new DeleteBranchCommand(id));
    }
}
