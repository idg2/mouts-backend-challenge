namespace Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Represents the branch returned after a successful creation.
/// </summary>
public class CreateBranchResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the created branch.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
