namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.DeleteBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Request model for deleting a branch.
/// </summary>
public class DeleteBranchRequest
{
    /// <summary>
    /// The unique identifier of the branch to delete.
    /// </summary>
    public Guid Id { get; set; }
}
