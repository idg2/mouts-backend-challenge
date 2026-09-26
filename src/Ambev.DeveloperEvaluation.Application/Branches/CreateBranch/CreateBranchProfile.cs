using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Profile for mapping between the CreateBranch command, the Branch entity, and the result.
/// </summary>
public class CreateBranchProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for the CreateBranch operation.
    /// </summary>
    public CreateBranchProfile()
    {
        CreateMap<CreateBranchCommand, Branch>();
        CreateMap<Branch, CreateBranchResult>();
    }
}
