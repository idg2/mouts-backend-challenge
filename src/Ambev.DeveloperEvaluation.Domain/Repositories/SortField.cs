namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-024 (FEAT-011)
/// <summary>
/// One sort key of a list query.
/// </summary>
/// <param name="Field">The entity property name</param>
/// <param name="Descending">True to sort from the highest value down</param>
public sealed record SortField(string Field, bool Descending);
