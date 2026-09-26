using System.Collections;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.Unit.TestData;

// Work item: TD-039
/// <summary>
/// Builds entities the way EF Core loads them: through the non-public constructor and the setters, whatever their
/// visibility. For tests that need a stored state the domain API does not produce, such as ids and numbers assigned by
/// the database or amounts from an earlier pricing.
/// </summary>
public static class Persisted
{
    /// <summary>
    /// Creates an entity and copies every property of <paramref name="values"/> onto the property of the same name.
    /// Each value must have the property's exact type (for example <c>7L</c> for a <see cref="long"/>).
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="values">An anonymous object with the property values</param>
    /// <returns>The entity</returns>
    public static T New<T>(object values) where T : class
    {
        var target = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        foreach (var property in values.GetType().GetProperties())
            Set(target, property.Name, property.GetValue(values));
        return target;
    }

    /// <summary>
    /// Sets one property through its setter, whatever its visibility. A collection property without a setter is
    /// filled through its backing field, named after the property in camel case with a leading underscore.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="target">The entity</param>
    /// <param name="name">The property name</param>
    /// <param name="value">The value, of the property's exact type</param>
    /// <returns>The entity</returns>
    public static T Set<T>(T target, string name, object? value) where T : class
    {
        var property = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new ArgumentException($"{typeof(T).Name} has no public property {name}", nameof(name));
        if (property.SetMethod is not null)
        {
            property.SetValue(target, value);
            return target;
        }

        var fieldName = "_" + char.ToLowerInvariant(name[0]) + name[1..];
        var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new ArgumentException($"{typeof(T).Name}.{name} has no setter and no {fieldName} field", nameof(name));
        var list = (IList)field.GetValue(target)!;
        var elements = ((IEnumerable)value!).Cast<object>().ToList();
        list.Clear();
        foreach (var element in elements)
            list.Add(element);
        return target;
    }
}
