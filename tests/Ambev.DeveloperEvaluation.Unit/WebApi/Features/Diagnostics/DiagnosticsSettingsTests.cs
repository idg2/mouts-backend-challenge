#if DEBUG
using Ambev.DeveloperEvaluation.WebApi.Features.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Diagnostics;

// Work item: TASK-082 (FEAT-019)
/// <summary>
/// Contains unit tests for the <see cref="DiagnosticsSettings"/> class.
/// </summary>
public class DiagnosticsSettingsTests
{
    // Work item: TASK-082 (FEAT-019)
    [Fact(DisplayName = "Given both keys When reading the settings Then the values are returned")]
    public void Given_BothKeys_When_Reading_Then_ValuesReturned()
    {
        // Arrange
        var configuration = Configuration(("Diagnostics:Trace:Enabled", "true"), ("Diagnostics:Trace:Capacity", "5000"));

        // Act
        var settings = DiagnosticsSettings.FromConfiguration(configuration);

        // Assert
        settings.TraceEnabled.Should().BeTrue();
        settings.TraceCapacity.Should().Be(5000);
    }

    // Work item: TASK-082 (FEAT-019)
    [Theory(DisplayName = "Given a missing or invalid key When reading the settings Then it throws naming the key")]
    [InlineData(null, "5000", "Diagnostics:Trace:Enabled")]
    [InlineData("yes", "5000", "Diagnostics:Trace:Enabled")]
    [InlineData("true", null, "Diagnostics:Trace:Capacity")]
    [InlineData("true", "0", "Diagnostics:Trace:Capacity")]
    [InlineData("true", "abc", "Diagnostics:Trace:Capacity")]
    public void Given_MissingOrInvalidKey_When_Reading_Then_ThrowsNamingKey(string? enabled, string? capacity, string key)
    {
        // Arrange
        var configuration = Configuration(("Diagnostics:Trace:Enabled", enabled), ("Diagnostics:Trace:Capacity", capacity));

        // Act
        var read = () => DiagnosticsSettings.FromConfiguration(configuration);

        // Assert
        read.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Where(pair => pair.Value is not null)
                .Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
}
#endif
