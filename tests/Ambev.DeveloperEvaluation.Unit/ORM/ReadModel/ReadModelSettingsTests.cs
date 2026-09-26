using Ambev.DeveloperEvaluation.ORM.ReadModel;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// Contains unit tests for the <see cref="ReadModelSettings"/> class.
/// </summary>
public class ReadModelSettingsTests
{
    private const string ValidConnectionString = "mongodb://user:p%40ss@localhost:27017/?authSource=admin";

    /// <summary>
    /// Tests that a complete configuration yields its values.
    /// </summary>
    [Fact(DisplayName = "Given a complete configuration When reading Then returns its values")]
    public void Given_CompleteConfiguration_When_Reading_Then_ReturnsValues()
    {
        // Arrange
        var configuration = Build(ValidValues());

        // Act
        var settings = ReadModelSettings.FromConfiguration(configuration);

        // Assert
        settings.ConnectionString.Should().Be(ValidConnectionString);
        settings.Database.Should().Be("read_db");
        settings.Collection.Should().Be("sales");
    }

    /// <summary>
    /// Tests that each missing or blank key fails with a message naming it.
    /// </summary>
    [Theory(DisplayName = "Given a missing or blank key When reading Then throws naming the key")]
    [InlineData(ReadModelSettings.ConnectionStringKey, null)]
    [InlineData(ReadModelSettings.ConnectionStringKey, " ")]
    [InlineData(ReadModelSettings.DatabaseKey, null)]
    [InlineData(ReadModelSettings.DatabaseKey, " ")]
    [InlineData(ReadModelSettings.CollectionKey, null)]
    [InlineData(ReadModelSettings.CollectionKey, " ")]
    public void Given_MissingOrBlankKey_When_Reading_Then_ThrowsNamingKey(string key, string? value)
    {
        // Arrange
        var values = ValidValues();
        values[key] = value;
        var configuration = Build(values);

        // Act
        var act = () => ReadModelSettings.FromConfiguration(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"{key} is not configured*");
    }

    /// <summary>
    /// Tests that a malformed URL fails naming the key without echoing the URL.
    /// </summary>
    [Fact(DisplayName = "Given a malformed MongoDB URL When reading Then throws naming the key without the URL")]
    public void Given_MalformedUrl_When_Reading_Then_ThrowsWithoutUrl()
    {
        // Arrange
        var values = ValidValues();
        values[ReadModelSettings.ConnectionStringKey] = "mongodb://user:p@ss@localhost:27017";
        var configuration = Build(values);

        // Act
        var act = () => ReadModelSettings.FromConfiguration(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().StartWith(ReadModelSettings.ConnectionStringKey).And.NotContain("p@ss");
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        [ReadModelSettings.ConnectionStringKey] = ValidConnectionString,
        [ReadModelSettings.DatabaseKey] = "read_db",
        [ReadModelSettings.CollectionKey] = "sales"
    };

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
