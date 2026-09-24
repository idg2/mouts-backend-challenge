namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// API response model for one branch in the ListBranches page.
/// </summary>
public class ListBranchesResponse
{
    /// <summary>
    /// The unique identifier of the branch.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
