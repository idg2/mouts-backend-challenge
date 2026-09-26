using Ambev.DeveloperEvaluation.DevConsole;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Contains unit tests for the console <see cref="CommandLine"/> parser.
/// </summary>
public class CommandLineTests
{
    [Fact(DisplayName = "Given no arguments When parsing Then the console must ask")]
    public void Given_NoArguments_When_Parsing_Then_Asks()
    {
        var parsed = CommandLine.Parse([]);
        parsed.Command.Should().BeNull();
        parsed.Scenario.Should().BeNull();
        parsed.Yes.Should().BeFalse();
        parsed.ConfigurationArgs.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given t, a scenario, --yes and settings When parsing Then each lands in its place")]
    public void Given_TraceArguments_When_Parsing_Then_Split()
    {
        var parsed = CommandLine.Parse(["t", "sale-async", "--yes", "--Trace:WaitTimeout=00:01:00"]);
        parsed.Command.Should().Be("t");
        parsed.Scenario.Should().Be("sale-async");
        parsed.Yes.Should().BeTrue();
        parsed.ConfigurationArgs.Should().Equal("--Trace:WaitTimeout=00:01:00");
    }

    [Fact(DisplayName = "Given l with simulator settings When parsing Then the settings pass through")]
    public void Given_LoadArguments_When_Parsing_Then_PassThrough()
    {
        var parsed = CommandLine.Parse(["l", "--Simulator:Mode=async"]);
        parsed.Command.Should().Be("l");
        parsed.ConfigurationArgs.Should().Equal("--Simulator:Mode=async");
    }

    [Fact(DisplayName = "Given a stray argument When parsing Then it throws naming it")]
    public void Given_StrayArgument_When_Parsing_Then_Throws()
    {
        var act = () => CommandLine.Parse(["l", "extra"]);
        act.Should().Throw<InvalidOperationException>().WithMessage("*extra*");
    }
}
