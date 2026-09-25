using System.Text.Json;
using Ambev.DeveloperEvaluation.DevConsole.Trace;
using Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-058 (FEAT-017)
/// <summary>
/// Contains unit tests for the registry entries of the <see cref="ScenarioCatalog"/>.
/// </summary>
public class ScenarioCatalogTests
{
    // Work item: TASK-058 (FEAT-017)
    [Theory(DisplayName = "Given many bodies of one run When creating customers or branches Then no two share a name")]
    [InlineData("customers")]
    [InlineData("branches")]
    public void Given_ManyBodiesOfOneRun_When_Creating_Then_NamesDistinct(string registry)
    {
        // Arrange
        var scenario = (RegistryScenario)ScenarioCatalog.All.Single(candidate => candidate.Name == registry);
        using var client = new HttpClient();
        var context = new ScenarioContext(client, string.Empty, new StepWaiter(), TimeSpan.FromSeconds(1), "run1", TextWriter.Null, "a@b.c", "p");

        // Act
        var names = Enumerable.Range(0, 200)
            .Select(_ => JsonSerializer.SerializeToElement(scenario.CreateBody(context)).GetProperty("name").GetString())
            .ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    // Work item: TASK-058 (FEAT-017)
    [Theory(DisplayName = "Given a registry When reading its description Then it mentions duplicate only with a unique field")]
    [InlineData("customers", true)]
    [InlineData("branches", false)]
    [InlineData("products", true)]
    public void Given_Registry_When_ReadingDescription_Then_DuplicateOnlyWithUniqueField(string registry, bool mentionsDuplicate)
    {
        // Act
        var description = ScenarioCatalog.All.Single(candidate => candidate.Name == registry).Description;

        // Assert
        description.Contains("duplicate").Should().Be(mentionsDuplicate);
    }
}
