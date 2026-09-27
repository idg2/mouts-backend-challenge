using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Tracing;

// Work item: TASK-094 (FEAT-020)
/// <summary>
/// Keeps the documented keys that the guided validation UI shows in its flows and steps equal to the keys of docs:
/// a key renamed or removed in docs must fail here instead of showing a stale key on the page.
/// </summary>
public class ValidationUiKeyTests
{
    private static readonly Regex StepKey = new(@"\b[A-Z]{3}-[A-Z]{3}(-\d{2})?\b", RegexOptions.Compiled);
    private static readonly Regex QuotedKey = new(@"'([A-Z]{3}-[A-Z]{3}(?:-\d{2})?)'", RegexOptions.Compiled);

    // Work item: TASK-094 (FEAT-020)
    [Fact(DisplayName = "Given the validation UI scenarios When comparing with the documents Then every key they show is documented")]
    public void Given_ValidationUiScenarios_When_ComparedWithDocs_Then_EveryKeyIsDocumented()
    {
        // Arrange
        var documented = Directory.GetFiles(Path.Combine(RepositoryDirectory(), "docs"), "*.md")
            .Where(path => Path.GetFileName(path) != "TEMPLATE.md")
            .SelectMany(path => StepKey.Matches(File.ReadAllText(path)).Select(match => match.Value))
            .ToHashSet();

        // Act
        var used = UiKeys();
        var unknown = used.Except(documented).Order().ToList();

        // Assert
        used.Should().NotBeEmpty("the scenarios name the documented keys of their flows");
        unknown.Should().BeEmpty($"every key the UI shows must exist in docs; unknown: {string.Join(", ", unknown)}");
    }

    private static HashSet<string> UiKeys() =>
        Directory.GetFiles(Path.Combine(RepositoryDirectory(), "tools", "validation-ui", "src", "app", "scenarios"), "*.ts", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".spec.ts", StringComparison.Ordinal))
            .SelectMany(path => QuotedKey.Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet();

    private static string RepositoryDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ambev.DeveloperEvaluation.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate Ambev.DeveloperEvaluation.sln from the test output directory");
    }
}
