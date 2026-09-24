namespace Ambev.DeveloperEvaluation.WebApi.Features.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Request model for listing branches, bound from the _page and _size query parameters.
/// </summary>
public class ListBranchesRequest
{
    /// <summary>
    /// The page number, starting at 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// The page size, from 1 to 100.
    /// </summary>
    public int Size { get; set; } = 10;
}
