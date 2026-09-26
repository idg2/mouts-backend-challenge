using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;

// Work item: TASK-019 (FEAT-010), TD-006
/// <summary>
/// Command for deleting a branch.
/// </summary>
public record DeleteBranchCommand : IRequest<DeleteBranchResult>, ITransactionalCommand
{
    /// <summary>
    /// The unique identifier of the branch to delete.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of DeleteBranchCommand.
    /// </summary>
    /// <param name="id">The ID of the branch to delete</param>
    public DeleteBranchCommand(Guid id)
    {
        Id = id;
    }
}
