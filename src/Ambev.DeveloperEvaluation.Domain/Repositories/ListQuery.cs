namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-024 (FEAT-011)
/// <summary>
/// One page of a list, narrowed by filters and sorted by sort fields.
/// </summary>
public sealed class ListQuery
{
    /// <summary>
    /// Gets the page number, starting at 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int Size { get; init; } = 10;

    /// <summary>
    /// Gets the filters.
    /// </summary>
    public IReadOnlyList<FieldFilter> Filters { get; init; } = [];

    /// <summary>
    /// Gets the sort fields; empty means the list's default order.
    /// </summary>
    public IReadOnlyList<SortField> Order { get; init; } = [];
}
