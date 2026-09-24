using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010), TD-006
/// <summary>
/// Command for updating an existing branch.
/// </summary>
public class UpdateBranchCommand : IRequest<UpdateBranchResult>, ITransactionalCommand
{
    /// <summary>
    /// Gets or sets the unique identifier of the branch to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
