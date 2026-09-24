using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Command for retrieving one page of branches ordered by name.
/// </summary>
public class ListBranchesCommand : IRequest<ListBranchesResult>
{
    /// <summary>
    /// Gets or sets the page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;
}
