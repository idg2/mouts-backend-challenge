namespace Ambev.DeveloperEvaluation.Integration;

// Work item: FEAT-012
/// <summary>
/// Hands out customer documents that are unique within a test run, for rows stored directly through the context or a
/// repository, where only the unique index applies. They are not valid CPFs or CNPJs.
/// </summary>
internal static class TestDocuments
{
    /// <summary>
    /// Returns a new 14-character document.
    /// </summary>
    public static string Next() => Guid.NewGuid().ToString("N")[..14].ToUpperInvariant();
}
