namespace Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Represents the branch returned after a successful update.
/// </summary>
public class UpdateBranchResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the branch.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
