namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.UpdateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// API response model for the UpdateBranch operation.
/// </summary>
public class UpdateBranchResponse
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
