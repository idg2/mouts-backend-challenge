using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

// Work item: TASK-026 (FEAT-011)
/// <summary>
/// Reads the field filters, ranges, and _order of a list request (general-api.md) against the public properties of
/// the list response, and throws a ValidationException that lists every problem found.
/// </summary>
public static class ListQueryParser
{
    private const string OrderKey = "_order";
    private const string MinPrefix = "_min";
    private const string MaxPrefix = "_max";
    private const string DayFormat = "yyyy-MM-dd";
    private const int MaxValuesPerField = 50;
    private const DateTimeStyles UtcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
    private const NumberStyles DecimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private static readonly HashSet<string> PagingKeys = new(StringComparer.OrdinalIgnoreCase) { "_page", "_size", OrderKey };
    private static readonly HashSet<Type> RangeTypes = [typeof(decimal), typeof(long), typeof(DateTime)];
    private static readonly string[] InstantFormats = ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", "yyyy-MM-dd'T'HH:mmK"];

    /// <summary>
    /// Gets the property types a list response may expose.
    /// </summary>
    public static IReadOnlySet<Type> SupportedTypes { get; } =
        new HashSet<Type> { typeof(string), typeof(Guid), typeof(bool), typeof(decimal), typeof(long), typeof(DateTime) };

    /// <summary>
    /// Parses the filters and sort fields of a list request. Paging keys are ignored.
    /// </summary>
    /// <typeparam name="TResponse">The list response whose public properties are the allowed fields</typeparam>
    /// <param name="query">The request query</param>
    /// <returns>The filters and sort fields, on entity property names</returns>
    /// <exception cref="ValidationException">When any key or value is invalid</exception>
    public static (IReadOnlyList<FieldFilter> Filters, IReadOnlyList<SortField> Order) Parse<TResponse>(IQueryCollection query)
    {
        var fields = typeof(TResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
        var filters = new List<FieldFilter>();
        var failures = new List<ValidationFailure>();

        foreach (var (key, values) in query)
        {
            if (PagingKeys.Contains(key))
                continue;

            if (key.StartsWith(MinPrefix, StringComparison.OrdinalIgnoreCase) || key.StartsWith(MaxPrefix, StringComparison.OrdinalIgnoreCase))
                AddRange(key, values, fields, filters, failures);
            else if (key.StartsWith('_'))
                failures.Add(Failure(key, "UnknownParameter", $"Unknown query parameter '{key}'."));
            else if (!fields.TryGetValue(key, out var field))
                failures.Add(Failure(key, "UnknownField", $"Unknown filter field '{key}'."));
            else if (values.Count > MaxValuesPerField)
                failures.Add(Failure(key, "TooManyValues", $"'{key}' may appear at most {MaxValuesPerField} times."));
            else
                foreach (var value in values)
                    AddMatch(key, value ?? string.Empty, field, filters, failures);
        }

        var order = ParseOrder(query[OrderKey], fields, failures);

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return (filters, order);
    }

    private static void AddMatch(string key, string value, PropertyInfo field, List<FieldFilter> filters, List<ValidationFailure> failures)
    {
        if (field.PropertyType == typeof(string))
        {
            var pattern = ToLikePattern(value);
            if (pattern == null)
                failures.Add(Failure(key, "InvalidWildcard", $"'{key}' accepts '*' only at the start or the end of the value."));
            else
                filters.Add(new FieldFilter(field.Name, FilterOperator.Like, pattern));
            return;
        }

        if (value.Contains('*'))
            failures.Add(Failure(key, "InvalidWildcard", $"'{key}' is not a text field and does not accept '*'."));
        else if (field.PropertyType == typeof(DateTime) && TryParseDay(value, out var day))
            filters.Add(new FieldFilter(field.Name, FilterOperator.OnDay, day));
        else if (TryConvert(value, field.PropertyType, out var converted))
            filters.Add(new FieldFilter(field.Name, FilterOperator.Equal, converted));
        else
            failures.Add(InvalidValue(key, value, field.PropertyType));
    }

    private static void AddRange(
        string key, StringValues values, Dictionary<string, PropertyInfo> fields, List<FieldFilter> filters, List<ValidationFailure> failures)
    {
        var isMin = key.StartsWith(MinPrefix, StringComparison.OrdinalIgnoreCase);
        var name = key[MinPrefix.Length..];

        if (!fields.TryGetValue(name, out var field))
        {
            failures.Add(Failure(key, "UnknownField", $"Unknown filter field '{name}'."));
            return;
        }

        if (!RangeTypes.Contains(field.PropertyType))
        {
            failures.Add(Failure(key, "InvalidRange", $"'{name}' is not a number or a date and has no range."));
            return;
        }

        if (values.Count != 1)
        {
            failures.Add(Failure(key, "RepeatedRange", $"'{key}' may appear only once."));
            return;
        }

        var value = values[0] ?? string.Empty;
        if (field.PropertyType == typeof(DateTime) && TryParseDay(value, out var day))
            filters.Add(isMin
                ? new FieldFilter(field.Name, FilterOperator.GreaterThanOrEqual, day)
                : new FieldFilter(field.Name, FilterOperator.LessThan, day.AddDays(1)));
        else if (TryConvert(value, field.PropertyType, out var bound))
            filters.Add(new FieldFilter(field.Name, isMin ? FilterOperator.GreaterThanOrEqual : FilterOperator.LessThanOrEqual, bound));
        else
            failures.Add(InvalidValue(key, value, field.PropertyType));
    }

    private static List<SortField> ParseOrder(StringValues values, Dictionary<string, PropertyInfo> fields, List<ValidationFailure> failures)
    {
        var order = new List<SortField>();
        if (values.Count > 1)
        {
            failures.Add(Failure(OrderKey, "RepeatedOrder", "'_order' may appear only once."));
            return order;
        }

        var text = (values.Count == 1 ? values[0] : null)?.Trim() ?? string.Empty;
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
            text = text[1..^1].Trim();
        if (text.Length == 0)
            return order;

        foreach (var entry in text.Split(','))
        {
            var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                failures.Add(Failure(OrderKey, "InvalidOrder", "'_order' has an empty entry."));
                continue;
            }

            if (!fields.TryGetValue(parts[0], out var field))
            {
                failures.Add(Failure(OrderKey, "UnknownField", $"Unknown order field '{parts[0]}'."));
                continue;
            }

            var direction = parts.Length > 1 ? parts[1] : "asc";
            var descending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
            if (parts.Length > 2 || !(descending || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add(Failure(OrderKey, "InvalidOrder", $"'{entry.Trim()}' must be a field optionally followed by asc or desc."));
                continue;
            }

            if (order.Exists(sort => sort.Field == field.Name))
            {
                failures.Add(Failure(OrderKey, "InvalidOrder", $"'{parts[0]}' appears more than once in '_order'."));
                continue;
            }

            order.Add(new SortField(field.Name, descending));
        }

        return order;
    }

    private static string? ToLikePattern(string value)
    {
        if (value.Length > 0 && value.All(character => character == '*'))
            return "%";

        var leading = value.StartsWith('*');
        var trailing = value.EndsWith('*');
        var literal = value[(leading ? 1 : 0)..(value.Length - (trailing ? 1 : 0))];
        if (literal.Contains('*'))
            return null;

        var escaped = literal.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return (leading ? "%" : string.Empty) + escaped + (trailing ? "%" : string.Empty);
    }

    // The last day has no next midnight, so it cannot be a whole-day filter or an exclusive upper bound.
    private static bool TryParseDay(string value, out DateTime day) =>
        DateTime.TryParseExact(value, DayFormat, CultureInfo.InvariantCulture, UtcStyles, out day) && day < DateTime.MaxValue.Date;

    private static bool TryConvert(string value, Type type, out object converted)
    {
        var invariant = CultureInfo.InvariantCulture;
        converted = null!;

        if (type == typeof(Guid) && Guid.TryParse(value, out var id))
            converted = id;
        else if (type == typeof(bool) && bool.TryParse(value, out var flag))
            converted = flag;
        else if (type == typeof(decimal) && decimal.TryParse(value, DecimalStyles, invariant, out var number))
            converted = number;
        else if (type == typeof(long) && long.TryParse(value, NumberStyles.AllowLeadingSign, invariant, out var integer))
            converted = integer;
        else if (type == typeof(DateTime) && DateTime.TryParseExact(value, InstantFormats, invariant, UtcStyles, out var instant))
            converted = instant;

        return converted != null;
    }

    private static ValidationFailure InvalidValue(string key, string value, Type type)
    {
        var expected = type == typeof(Guid) ? "an id"
            : type == typeof(bool) ? "true or false"
            : type == typeof(decimal) ? "a number with '.' as the decimal separator"
            : type == typeof(long) ? "a whole number"
            : "a date (yyyy-MM-dd) or a date and time (yyyy-MM-ddTHH:mm:ssZ)";
        return Failure(key, "InvalidValue", $"'{value}' is not valid for '{key}': expected {expected}.");
    }

    private static ValidationFailure Failure(string key, string errorCode, string message) =>
        new(key, message) { ErrorCode = errorCode };
}
