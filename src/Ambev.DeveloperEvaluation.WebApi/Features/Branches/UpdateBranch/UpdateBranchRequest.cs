namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Represents a request to update a branch.
/// </summary>
public class UpdateBranchRequest
{
    /// <summary>
    /// Gets or sets the branch id. The controller sets it from the route.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new branch name. Required, at most 100 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
