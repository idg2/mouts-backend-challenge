using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Tracing;

// Work item: TASK-043 (FEAT-017)
/// <summary>
/// Keeps the step keys of backend/docs and the StepTrace calls of src equal, in both directions.
/// </summary>
public class StepKeyCoverageTests
{
    private static readonly Regex StepKey = new(@"\b[A-Z]{3}-[A-Z]{3}-\d{2}\b", RegexOptions.Compiled);

    private static readonly Regex QuotedStepKey = new("\"([A-Z]{3}-[A-Z]{3}-\\d{2})\"", RegexOptions.Compiled);

    [Theory(DisplayName = "Given a document When comparing with the code Then every documented key is traced")]
    [InlineData("conventions.md")]
    [InlineData("auth.md")]
    [InlineData("users.md")]
    [InlineData("customers.md")]
    [InlineData("branches.md")]
    [InlineData("products.md")]
    [InlineData("sales.md")]
    public void Given_Document_When_ComparedWithCode_Then_EveryKeyIsTraced(string document)
    {
        // Arrange
        var documented = KeysIn(File.ReadAllText(Path.Combine(DocsDirectory(), document)));
        var traced = TracedKeys();

        // Act
        var missing = documented.Except(traced).Order().ToList();

        // Assert
        missing.Should().BeEmpty($"every key of {document} needs a StepTrace call; missing: {string.Join(", ", missing)}");
    }

    [Fact(DisplayName = "Given the code When comparing with the documents Then no traced key is unknown")]
    public void Given_TracedKeys_When_ComparedWithDocs_Then_NoUnknownKey()
    {
        // Arrange
        var documented = Directory.GetFiles(DocsDirectory(), "*.md")
            .Where(path => Path.GetFileName(path) != "TEMPLATE.md")
            .SelectMany(path => KeysIn(File.ReadAllText(path)))
            .ToHashSet();

        // Act
        var unknown = TracedKeys().Except(documented).Order().ToList();

        // Assert
        unknown.Should().BeEmpty($"a traced key must exist in backend/docs; unknown: {string.Join(", ", unknown)}");
    }

    private static HashSet<string> KeysIn(string text) =>
        StepKey.Matches(text).Select(match => match.Value).ToHashSet();

    // Keys count only as quoted string literals (a StepTrace.Step argument or a StepKeys entry), so a key in a
    // comment never satisfies the test.
    private static HashSet<string> TracedKeys() =>
        Directory.GetFiles(SourceDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(path => QuotedStepKey.Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet();

    private static string DocsDirectory() => Path.Combine(BackendDirectory(), "docs");

    private static string SourceDirectory() => Path.Combine(BackendDirectory(), "src");

    private static string BackendDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ambev.DeveloperEvaluation.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate Ambev.DeveloperEvaluation.sln from the test output directory");
    }
}
