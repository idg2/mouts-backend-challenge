namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-024 (FEAT-011)
/// <summary>
/// How a <see cref="FieldFilter"/> compares an entity property with its value.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Case-insensitive match of a text property against an ILIKE pattern escaped with a backslash.
    /// </summary>
    Like,

    /// <summary>
    /// The property equals the value.
    /// </summary>
    Equal,

    /// <summary>
    /// The date property falls on the UTC day that starts at the value.
    /// </summary>
    OnDay,

    /// <summary>
    /// The property is greater than or equal to the value.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// The property is less than or equal to the value.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// The property is less than the value.
    /// </summary>
    LessThan
}
