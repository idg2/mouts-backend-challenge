using System.Text.Json;
using Ambev.DeveloperEvaluation.DevConsole.Trace;
using Ambev.DeveloperEvaluation.DevConsole.Trace.Scenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-058 (FEAT-017), TASK-065 (FEAT-001)
/// <summary>
/// Contains unit tests for the registry entries of the <see cref="ScenarioCatalog"/>.
/// </summary>
public class ScenarioCatalogTests
{
    // Work item: TASK-058 (FEAT-017), TD-030
    [Theory(DisplayName = "Given many bodies of one run When creating customers or branches Then no two share a name")]
    [InlineData("customers")]
    [InlineData("branches")]
    public void Given_ManyBodiesOfOneRun_When_Creating_Then_NamesDistinct(string registry)
    {
        // Arrange
        var scenario = (RegistryScenario)ScenarioCatalog.All.Single(candidate => candidate.Name == registry);
        using var client = new HttpClient();
        var context = new ScenarioContext(client, string.Empty, new StepWaiter(), TimeSpan.FromSeconds(1), "run1", TextWriter.Null, "a@b.c", "p",
            new ServiceCollection().BuildServiceProvider(), new TraceFaults());

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

    // Work item: TASK-065 (FEAT-001)
    [Fact(DisplayName = "Given the catalog When listing the names Then discount-policy and sale-discount appear once, policies before sales")]
    public void Given_Catalog_When_ListingNames_Then_DiscountScenariosPresent()
    {
        // Act
        var names = ScenarioCatalog.All.Select(scenario => scenario.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
        names.Should().Contain(["discount-policy", "sale-discount"]);
        names.IndexOf("discount-policy").Should().BeLessThan(names.IndexOf("sale-create"));
    }

    // Work item: TD-030
    [Fact(DisplayName = "Given the catalog When a full run is summarized Then the failures scenario runs last and no documented key is an expected miss")]
    public void Given_Catalog_When_FullRunSummarized_Then_FailuresLastAndNoExpectedMiss()
    {
        // Act
        var last = ScenarioCatalog.All[^1].Name;

        // Assert
        last.Should().Be("failures");
        ScenarioCatalog.ExpectedMisses.Should().BeEmpty();
    }

    // Work item: TASK-065 (FEAT-001)
    [Fact(DisplayName = "Given explicit lines When building a sale body Then a missing discount is null and no amount is sent")]
    public void Given_Lines_When_BuildingBody_Then_NullDiscountAndNoAmounts()
    {
        // Arrange
        var references = new SaleReferences(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()]);

        // Act
        var body = JsonSerializer.SerializeToElement(SaleFixtures.LinesBody(references, (references.ProductIds[0], 4, null)));

        // Assert
        body.TryGetProperty("totalAmount", out _).Should().BeFalse();
        var item = body.GetProperty("items")[0];
        item.GetProperty("quantity").GetInt32().Should().Be(4);
        item.GetProperty("discountPercentage").ValueKind.Should().Be(JsonValueKind.Null);
        item.TryGetProperty("discountAmount", out _).Should().BeFalse();
        item.TryGetProperty("totalAmount", out _).Should().BeFalse();
    }

    // Work item: TASK-065 (FEAT-001)
    [Fact(DisplayName = "Given a policy body When serializing Then validFrom ends in Z and each tier carries its bounds")]
    public void Given_PolicyBody_When_Serializing_Then_UtcAndTiers()
    {
        // Arrange
        var validFrom = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var body = JsonSerializer.SerializeToElement(DiscountPolicyScenario.PolicyBody(null, validFrom, 20, (4, 9, 10m), (10, null, 20m)));

        // Assert
        body.GetProperty("validFrom").GetString().Should().Be("2026-10-01T00:00:00Z");
        body.GetProperty("maxQuantityPerProduct").GetInt32().Should().Be(20);
        body.GetProperty("tiers").GetArrayLength().Should().Be(2);
        body.GetProperty("tiers")[1].GetProperty("maxQuantity").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
