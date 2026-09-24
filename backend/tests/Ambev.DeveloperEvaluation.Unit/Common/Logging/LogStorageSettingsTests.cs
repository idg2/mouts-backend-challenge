using Ambev.DeveloperEvaluation.Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Logging;

// Work item: TASK-033 (FEAT-016)
/// <summary>
/// Contains unit tests for the <see cref="LogStorageSettings"/> class.
/// </summary>
public class LogStorageSettingsTests
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
        var settings = LogStorageSettings.FromConfiguration(configuration);

        // Assert
        settings.ConnectionString.Should().Be(ValidConnectionString);
        settings.Database.Should().Be("logs_db");
        settings.Collection.Should().Be("logs");
        settings.ExpireAfter.Should().Be(TimeSpan.FromDays(1));
    }

    /// <summary>
    /// Tests that each missing or blank key fails with a message naming it.
    /// </summary>
    [Theory(DisplayName = "Given a missing or blank key When reading Then throws naming the key")]
    [InlineData(LogStorageSettings.ConnectionStringKey, null)]
    [InlineData(LogStorageSettings.ConnectionStringKey, "  ")]
    [InlineData(LogStorageSettings.DatabaseKey, null)]
    [InlineData(LogStorageSettings.DatabaseKey, "  ")]
    [InlineData(LogStorageSettings.CollectionKey, null)]
    [InlineData(LogStorageSettings.CollectionKey, "  ")]
    [InlineData(LogStorageSettings.ExpireAfterKey, null)]
    [InlineData(LogStorageSettings.ExpireAfterKey, "  ")]
    public void Given_MissingOrBlankKey_When_Reading_Then_ThrowsNamingTheKey(string key, string? value)
    {
        // Arrange
        var values = ValidValues();
        if (value is null)
            values.Remove(key);
        else
            values[key] = value;

        // Act
        var act = () => LogStorageSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    /// <summary>
    /// Tests that an ExpireAfter that is not a positive time span is rejected.
    /// </summary>
    [Theory(DisplayName = "Given an ExpireAfter that is not a positive time span When reading Then throws naming the key")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("00:00:00")]
    [InlineData("-01:00:00")]
    public void Given_InvalidExpireAfter_When_Reading_Then_ThrowsNamingTheKey(string value)
    {
        // Arrange
        var values = ValidValues();
        values[LogStorageSettings.ExpireAfterKey] = value;

        // Act
        var act = () => LogStorageSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{LogStorageSettings.ExpireAfterKey}*");
    }

    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// Tests that a connection string the MongoDB driver cannot parse is rejected without echoing the password.
    /// </summary>
    [Theory(DisplayName = "Given a connection string that is not a MongoDB URL When reading Then throws naming the key without the password")]
    [InlineData("http://user:s3cret@localhost")]
    [InlineData("mongodb://developer:s3cret@x@localhost:27017/?authSource=admin")]
    [InlineData("mongodb://developer:ab/cds3cret@localhost:27017/?authSource=admin")]
    [InlineData("Host=localhost;Password=s3cret")]
    public void Given_InvalidMongoUrl_When_Reading_Then_ThrowsNamingTheKeyWithoutThePassword(string connectionString)
    {
        // Arrange
        var values = ValidValues();
        values[LogStorageSettings.ConnectionStringKey] = connectionString;

        // Act
        var act = () => LogStorageSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(LogStorageSettings.ConnectionStringKey).And.NotContain("s3cret");
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        [LogStorageSettings.ConnectionStringKey] = ValidConnectionString,
        [LogStorageSettings.DatabaseKey] = "logs_db",
        [LogStorageSettings.CollectionKey] = "logs",
        [LogStorageSettings.ExpireAfterKey] = "1.00:00:00"
    };

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
