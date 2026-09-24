using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging;

// Work item: TASK-038 (FEAT-006)
/// <summary>
/// Contains unit tests for the <see cref="MessagingSettings"/> class.
/// </summary>
public class MessagingSettingsTests
{
    private const string ValidConnectionString = "mongodb://user:p%40ss@localhost:27017/bus_db?authSource=admin";

    /// <summary>
    /// Tests that a complete configuration yields its values.
    /// </summary>
    [Fact(DisplayName = "Given a complete configuration When reading Then returns its values")]
    public void Given_CompleteConfiguration_When_Reading_Then_ReturnsValues()
    {
        // Arrange
        var configuration = Build(ValidValues());

        // Act
        var settings = MessagingSettings.FromConfiguration(configuration);

        // Assert
        settings.ConnectionString.Should().Be(ValidConnectionString);
        settings.InputQueue.Should().Be("sales-intake");
        settings.Workers.Should().Be(1);
        settings.MaxParallelism.Should().Be(20);
    }

    /// <summary>
    /// Tests that each missing or blank key fails with a message naming it.
    /// </summary>
    [Theory(DisplayName = "Given a missing or blank key When reading Then throws naming the key")]
    [InlineData(MessagingSettings.ConnectionStringKey, null)]
    [InlineData(MessagingSettings.ConnectionStringKey, "  ")]
    [InlineData(MessagingSettings.InputQueueKey, null)]
    [InlineData(MessagingSettings.InputQueueKey, "  ")]
    [InlineData(MessagingSettings.WorkersKey, null)]
    [InlineData(MessagingSettings.WorkersKey, "  ")]
    [InlineData(MessagingSettings.MaxParallelismKey, null)]
    [InlineData(MessagingSettings.MaxParallelismKey, "  ")]
    public void Given_MissingOrBlankKey_When_Reading_Then_ThrowsNamingTheKey(string key, string? value)
    {
        // Arrange
        var values = ValidValues();
        if (value is null)
            values.Remove(key);
        else
            values[key] = value;

        // Act
        var act = () => MessagingSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    /// <summary>
    /// Tests that worker and parallelism counts must be positive integers.
    /// </summary>
    [Theory(DisplayName = "Given a non-positive or non-numeric count When reading Then throws naming the key")]
    [InlineData(MessagingSettings.WorkersKey, "0")]
    [InlineData(MessagingSettings.WorkersKey, "-1")]
    [InlineData(MessagingSettings.WorkersKey, "abc")]
    [InlineData(MessagingSettings.MaxParallelismKey, "0")]
    [InlineData(MessagingSettings.MaxParallelismKey, "2.5")]
    [InlineData(MessagingSettings.MaxParallelismKey, "abc")]
    public void Given_InvalidCount_When_Reading_Then_ThrowsNamingTheKey(string key, string value)
    {
        // Arrange
        var values = ValidValues();
        values[key] = value;

        // Act
        var act = () => MessagingSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    /// <summary>
    /// Tests that a URL without a database path is rejected: the MongoDB transport stores the queue in that database.
    /// </summary>
    [Theory(DisplayName = "Given a MongoDB URL without a database When reading Then throws naming the key")]
    [InlineData("mongodb://user:pass@localhost:27017/?authSource=admin")]
    [InlineData("mongodb://localhost:27017")]
    public void Given_UrlWithoutDatabase_When_Reading_Then_ThrowsNamingTheKey(string connectionString)
    {
        // Arrange
        var values = ValidValues();
        values[MessagingSettings.ConnectionStringKey] = connectionString;

        // Act
        var act = () => MessagingSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{MessagingSettings.ConnectionStringKey}*database*");
    }

    /// <summary>
    /// Tests that a malformed URL fails naming the key without echoing the password.
    /// </summary>
    [Theory(DisplayName = "Given a malformed MongoDB URL When reading Then throws naming the key without the password")]
    [InlineData("mongodb://developer:s3cret@x@localhost:27017/?authSource=admin")]
    [InlineData("mongodb://developer:ab/cds3cret@localhost:27017/?authSource=admin")]
    [InlineData("Host=localhost;Password=s3cret")]
    public void Given_InvalidMongoUrl_When_Reading_Then_ThrowsNamingTheKeyWithoutThePassword(string connectionString)
    {
        // Arrange
        var values = ValidValues();
        values[MessagingSettings.ConnectionStringKey] = connectionString;

        // Act
        var act = () => MessagingSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(MessagingSettings.ConnectionStringKey).And.NotContain("s3cret");
    }

    /// <summary>
    /// Tests that a database name MongoDB refuses is rejected at startup instead of failing inside the transport.
    /// </summary>
    [Theory(DisplayName = "Given a database name MongoDB refuses When reading Then throws naming the key")]
    [InlineData("mongodb://localhost:27017/bad.db")]
    [InlineData("mongodb://localhost:27017/bad$db")]
    [InlineData("mongodb://localhost:27017/bad db")]
    public void Given_InvalidDatabaseName_When_Reading_Then_ThrowsNamingTheKey(string connectionString)
    {
        // Arrange
        var values = ValidValues();
        values[MessagingSettings.ConnectionStringKey] = connectionString;

        // Act
        var act = () => MessagingSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{MessagingSettings.ConnectionStringKey}*");
    }

    /// <summary>
    /// Tests that the queue cannot share the log database (spec D10).
    /// </summary>
    [Fact(DisplayName = "Given the queue database is the log database When reading Then throws naming both keys")]
    public void Given_QueueDatabaseIsLogDatabase_When_Reading_Then_ThrowsNamingBothKeys()
    {
        // Arrange
        var values = ValidValues();
        values[LogStorageSettings.DatabaseKey] = "bus_db";

        // Act
        var act = () => MessagingSettings.FromConfiguration(Build(values));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{MessagingSettings.ConnectionStringKey}*{LogStorageSettings.DatabaseKey}*");
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        [MessagingSettings.ConnectionStringKey] = ValidConnectionString,
        [MessagingSettings.InputQueueKey] = "sales-intake",
        [MessagingSettings.WorkersKey] = "1",
        [MessagingSettings.MaxParallelismKey] = "20"
    };

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
