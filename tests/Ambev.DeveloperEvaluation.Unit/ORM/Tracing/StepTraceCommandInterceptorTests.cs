#if DEBUG
using System.Data.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.ORM.Tracing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.ORM.Tracing;

// Work item: TASK-047 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTraceCommandInterceptor"/>.
/// </summary>
[Collection("StepTrace")]
public class StepTraceCommandInterceptorTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    // Work item: TASK-047 (FEAT-017)
    /// <summary>
    /// Installs a sink that collects every traced step.
    /// </summary>
    public StepTraceCommandInterceptorTests()
    {
        StepTrace.Sink = _events.Add;
    }

    // Work item: TASK-047 (FEAT-017)
    /// <summary>
    /// Removes the sink.
    /// </summary>
    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    // Work item: TASK-047 (FEAT-017)
    /// <summary>
    /// Tests that a non-query command prints CMN-PIP-11 with the first SQL line, the parameter count, and the rows,
    /// and that the interceptor returns the result unchanged.
    /// </summary>
    [Fact(DisplayName = "Given a non-query command When executed Then PIP-11 prints the first SQL line and the rows")]
    public void Given_NonQuery_When_Executed_Then_Pip11WithRows()
    {
        // Arrange
        using var connection = new NpgsqlConnection();
        using DbCommand command = new NpgsqlCommand("UPDATE \"Sales\" SET \"IsCancelled\" = @p0\nWHERE \"Id\" = @p1");
        command.Parameters.Add(new NpgsqlParameter("p0", true));
        command.Parameters.Add(new NpgsqlParameter("p1", Guid.NewGuid()));
        var definition = new EventDefinition(
            Substitute.For<ILoggingOptions>(), RelationalEventId.CommandExecuted, LogLevel.Debug, "x", _ => (_, _) => { });
        var eventData = new CommandExecutedEventData(
            definition, (_, _) => "x", connection, command, null, DbCommandMethod.ExecuteNonQuery, Guid.NewGuid(), Guid.NewGuid(),
            3, false, false, DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(4), CommandSource.SaveChanges);

        // Act
        var result = new StepTraceCommandInterceptor().NonQueryExecuted(command, eventData, 3);

        // Assert
        result.Should().Be(3);
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().Be("CMN-PIP-11");
        stepEvent.Values.Should().Contain(("kind", "ExecuteNonQuery"))
            .And.Contain(("sql", "UPDATE \"Sales\" SET \"IsCancelled\" = @p0"))
            .And.Contain(("parameters", "2"))
            .And.Contain(("rows", "3"));
    }
}
#endif
