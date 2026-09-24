namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-024 (FEAT-011)
/// <summary>
/// One condition of a list query on an entity property. The Like, Equal, and OnDay filters of one property are
/// combined with OR; every other filter is combined with AND.
/// </summary>
/// <param name="Field">The entity property name</param>
/// <param name="Operator">How the property is compared</param>
/// <param name="Value">The value, already converted to the property type; the escaped pattern for Like</param>
public sealed record FieldFilter(string Field, FilterOperator Operator, object Value);
