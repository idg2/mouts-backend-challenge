using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

// Work item: TASK-024 (FEAT-011), TASK-075 (FEAT-003)
/// <summary>
/// Translates the filters and sort fields of a <see cref="ListQuery"/> into a query over entity properties by name:
/// EF Core (values sent as SQL parameters) or, through the Like translator overload, the MongoDB read model.
/// </summary>
public static class ListQueryExtensions
{
    private const string LikeEscape = "\\";

    private static readonly MethodInfo ILikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
        nameof(NpgsqlDbFunctionsExtensions.ILike),
        [typeof(DbFunctions), typeof(string), typeof(string), typeof(string)])!;

    /// <summary>
    /// Applies the filters: the Like, Equal, and OnDay filters of one property as one OR, everything else with AND.
    /// Like uses the Npgsql ILIKE translation.
    /// </summary>
    /// <param name="source">The query to narrow</param>
    /// <param name="filters">The filters, on entity property names</param>
    /// <returns>The narrowed query</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> source, IReadOnlyList<FieldFilter> filters) =>
        source.ApplyFilters(filters, NpgsqlLike);

    // Work item: TASK-075 (FEAT-003)
    /// <summary>
    /// Applies the filters with a caller-supplied Like translation, so the same tree serves EF Core (ILIKE) and the
    /// MongoDB read model (a regular expression).
    /// </summary>
    /// <param name="source">The query to narrow</param>
    /// <param name="filters">The filters, on entity property names</param>
    /// <param name="like">Builds the predicate of a Like filter from the property and the ILIKE pattern</param>
    /// <returns>The narrowed query</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> source, IReadOnlyList<FieldFilter> filters, Func<MemberExpression, string, Expression> like)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        foreach (var field in filters.GroupBy(filter => filter.Field))
        {
            var property = Expression.Property(parameter, field.Key);

            var matches = field.Where(IsMatch).Select(filter => Compare(property, filter, like)).ToList();
            if (matches.Count > 0)
                source = source.Where(Expression.Lambda<Func<T, bool>>(AnyOf(matches, 0, matches.Count), parameter));

            foreach (var range in field.Where(filter => !IsMatch(filter)))
                source = source.Where(Expression.Lambda<Func<T, bool>>(Compare(property, range, like), parameter));
        }

        return source;
    }

    /// <summary>
    /// Orders by the sort fields, or by the default ones when there are none, and then by Id.
    /// </summary>
    /// <param name="source">The query to order</param>
    /// <param name="order">The requested sort fields, on entity property names</param>
    /// <param name="defaultOrder">The sort fields used when none are requested</param>
    /// <returns>The ordered query</returns>
    public static IQueryable<T> ApplyOrder<T>(this IQueryable<T> source, IReadOnlyList<SortField> order, IReadOnlyList<SortField> defaultOrder)
    {
        var keys = (order.Count > 0 ? order : defaultOrder).ToList();
        if (keys.TrueForAll(key => key.Field != "Id"))
            keys.Add(new SortField("Id", false));

        var parameter = Expression.Parameter(typeof(T), "entity");
        var expression = source.Expression;
        for (var index = 0; index < keys.Count; index++)
        {
            var property = Expression.Property(parameter, keys[index].Field);
            var method = (index == 0 ? "OrderBy" : "ThenBy") + (keys[index].Descending ? "Descending" : string.Empty);
            expression = Expression.Call(
                typeof(Queryable),
                method,
                [typeof(T), property.Type],
                expression,
                Expression.Quote(Expression.Lambda(property, parameter)));
        }

        return source.Provider.CreateQuery<T>(expression);
    }

    // Combines the terms with OR as a balanced tree, so the expression depth grows with log2 of the number of values
    // and EF Core's recursive visitors do not exhaust the stack.
    private static Expression AnyOf(List<Expression> terms, int start, int count) =>
        count == 1
            ? terms[start]
            : Expression.OrElse(AnyOf(terms, start, count / 2), AnyOf(terms, start + count / 2, count - count / 2));

    private static bool IsMatch(FieldFilter filter) =>
        filter.Operator is FilterOperator.Like or FilterOperator.Equal or FilterOperator.OnDay;

    // Work item: TASK-075 (FEAT-003)
    private static Expression NpgsqlLike(MemberExpression property, string pattern) =>
        Expression.Call(ILikeMethod, Expression.Constant(EF.Functions), property, Parameter(pattern, typeof(string)), Expression.Constant(LikeEscape));

    // Work item: TASK-075 (FEAT-003)
    private static Expression Compare(MemberExpression property, FieldFilter filter, Func<MemberExpression, string, Expression> like)
    {
        if (filter.Operator == FilterOperator.Like)
            return like(property, (string)filter.Value);

        var value = Parameter(filter.Value, property.Type);
        return filter.Operator switch
        {
            FilterOperator.Equal => Expression.Equal(property, value),
            FilterOperator.OnDay => Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, value),
                Expression.LessThan(property, Parameter(((DateTime)filter.Value).AddDays(1), property.Type))),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, value),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, value),
            FilterOperator.LessThan => Expression.LessThan(property, value),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter.Operator, "Unknown filter operator")
        };
    }

    // Reads the value through a holder object, as a closure would, so EF Core sends it as a parameter, not a literal.
    private static MemberExpression Parameter(object value, Type type)
    {
        var holder = Activator.CreateInstance(typeof(ValueHolder<>).MakeGenericType(type), value)!;
        return Expression.Property(Expression.Constant(holder), nameof(ValueHolder<object>.Value));
    }

    private sealed class ValueHolder<TValue>
    {
        public ValueHolder(TValue value)
        {
            Value = value;
        }

        public TValue Value { get; }
    }
}
