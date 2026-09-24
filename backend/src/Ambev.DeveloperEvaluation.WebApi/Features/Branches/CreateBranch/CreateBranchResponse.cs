namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.CreateBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// API response model for the CreateBranch operation.
/// </summary>
public class CreateBranchResponse
{
    /// <summary>
    /// The unique identifier of the created branch.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The branch name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
