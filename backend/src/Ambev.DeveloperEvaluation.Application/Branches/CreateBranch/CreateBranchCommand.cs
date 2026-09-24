using Ambev.DeveloperEvaluation.Application.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010), TD-006
/// <summary>
/// Command for creating a new branch.
/// </summary>
public class CreateBranchCommand : IRequest<CreateBranchResult>, ITransactionalCommand
{
    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
