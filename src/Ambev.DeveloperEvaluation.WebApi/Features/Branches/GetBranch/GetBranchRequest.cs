namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.GetBranch;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Request model for getting a branch by ID.
/// </summary>
public class GetBranchRequest
{
    /// <summary>
    /// The unique identifier of the branch to retrieve.
    /// </summary>
    public Guid Id { get; set; }
}
