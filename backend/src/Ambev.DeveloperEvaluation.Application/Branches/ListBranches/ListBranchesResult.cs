namespace Ambev.DeveloperEvaluation.Application.Branches.ListBranches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Response model for the ListBranches operation: one page of branches and the total count.
/// </summary>
public class ListBranchesResult
{
    /// <summary>
    /// The branches on the requested page.
    /// </summary>
    public List<ListBranchesItem> Items { get; set; } = [];

    /// <summary>
    /// The total number of branches.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The requested page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The requested page size.
    /// </summary>
    public int Size { get; set; }
}

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// A branch entry in the ListBranches result.
/// </summary>
public class ListBranchesItem
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
