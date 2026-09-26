using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Command for retrieving a branch by ID.
/// </summary>
public record GetBranchCommand : IRequest<GetBranchResult>
{
    /// <summary>
    /// The unique identifier of the branch to retrieve.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of GetBranchCommand.
    /// </summary>
    /// <param name="id">The ID of the branch to retrieve</param>
    public GetBranchCommand(Guid id)
    {
        Id = id;
    }
}
