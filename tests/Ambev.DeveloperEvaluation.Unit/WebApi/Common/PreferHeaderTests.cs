using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

// Work item: TASK-039 (FEAT-006)
/// <summary>
/// Contains unit tests for the <see cref="PreferHeader"/> class.
/// </summary>
public class PreferHeaderTests
{
    /// <summary>
    /// Tests that respond-async is found among comma-separated preferences, ignoring case, spaces, and parameters,
    /// and that nothing else counts.
    /// </summary>
    [Theory(DisplayName = "Given a Prefer value When reading it Then detects respond-async")]
    [InlineData("respond-async", true)]
    [InlineData("Respond-Async", true)]
    [InlineData(" respond-async ", true)]
    [InlineData("wait=10, respond-async", true)]
    [InlineData("respond-async; foo=bar", true)]
    [InlineData("return=minimal", false)]
    [InlineData("respond-asynchronously", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Given_PreferValue_When_Reading_Then_DetectsRespondAsync(string? prefer, bool expected)
    {
        // Act
        var result = PreferHeader.RequestsRespondAsync(prefer);

        // Assert
        result.Should().Be(expected);
    }
}
