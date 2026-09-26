using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Ambev.DeveloperEvaluation.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// Translates the Like filter for the MongoDB read model. The filter value is the ILIKE pattern the list parser
/// produces for PostgreSQL (`%` for `*`, and `\`, `%`, `_` escaped with a backslash); it becomes an anchored,
/// case-insensitive regular expression with the same meaning, built once per filter.
/// </summary>
public static class MongoLike
{
    private static readonly MethodInfo IsMatchMethod = typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string)])!;

    /// <summary>
    /// Converts an ILIKE pattern into a regular expression: `%` matches any text, `_` any one character, a
    /// backslash-escaped character matches itself, everything else is literal; anchored at both ends.
    /// </summary>
    /// <param name="likePattern">The ILIKE pattern</param>
    /// <returns>The regular expression text</returns>
    public static string Pattern(string likePattern)
    {
        var regex = new StringBuilder("^");
        for (var index = 0; index < likePattern.Length; index++)
        {
            var character = likePattern[index];
            switch (character)
            {
                case '\\' when index + 1 < likePattern.Length:
                    regex.Append(Regex.Escape(likePattern[++index].ToString()));
                    break;
                case '%':
                    regex.Append(".*");
                    break;
                case '_':
                    regex.Append('.');
                    break;
                default:
                    regex.Append(Regex.Escape(character.ToString()));
                    break;
            }
        }

        return regex.Append('$').ToString();
    }

    /// <summary>
    /// Builds `regex.IsMatch(property)` with the regular expression as a constant, the form the driver translates
    /// to a `$regex` filter.
    /// </summary>
    /// <param name="property">The text property being filtered</param>
    /// <param name="likePattern">The ILIKE pattern</param>
    /// <returns>The predicate expression</returns>
    public static Expression Translate(MemberExpression property, string likePattern) =>
        Expression.Call(Expression.Constant(new Regex(Pattern(likePattern), RegexOptions.IgnoreCase)), IsMatchMethod, property);
}
