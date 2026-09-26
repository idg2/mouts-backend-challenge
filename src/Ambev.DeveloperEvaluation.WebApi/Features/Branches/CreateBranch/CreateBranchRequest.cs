namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Represents a request to create a new branch.
/// </summary>
public class CreateBranchRequest
{
    /// <summary>
    /// Gets or sets the branch name. Required, at most 100 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
