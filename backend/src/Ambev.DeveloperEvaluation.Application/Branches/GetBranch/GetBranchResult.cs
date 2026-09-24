namespace Ambev.DeveloperEvaluation.Application.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Response model for the GetBranch operation.
/// </summary>
public class GetBranchResult
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
