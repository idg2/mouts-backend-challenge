using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.DevConsole.Trace;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.DevConsole;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="ConsoleSink"/>.
/// </summary>
public class ConsoleSinkTests
{
    [Fact(DisplayName = "Given an event When formatting Then the line has time, thread, keys, title, values, and file:line")]
    public void Given_Event_When_Formatting_Then_DocumentedLine()
    {
        // Arrange
        var at = new DateTime(2026, 9, 25, 14, 3, 21).AddTicks(4829130);
        var stepEvent = new StepEvent(at, 12, "SAL-CRT-05", "CMN-PIP-10", "Preset id already stored?", [("id", "0b6f"), ("stored", "False")], "/x/CreateSaleHandler.cs", 84);

        // Act
        var line = ConsoleSink.Format(stepEvent);

        // Assert
        line.Should().Be("14:03:21.482913  T012  SAL-CRT-05 CMN-PIP-10  Preset id already stored?  id=0b6f stored=False  CreateSaleHandler.cs:84");
    }

    [Fact(DisplayName = "Given the host is not ready When a PIP-11 event arrives Then it is dropped, others are kept")]
    public void Given_HostNotReady_When_Pip11_Then_Dropped()
    {
        // Arrange
        var output = new StringWriter();
        var waiter = new StepWaiter();
        var sink = new ConsoleSink(output, waiter);

        // Act
        sink.Handle(new StepEvent(DateTime.Now, 1, "CMN-PIP-11", null, "sql", [], "f.cs", 1));
        sink.Handle(new StepEvent(DateTime.Now, 1, "USR-SED-01", null, "seed", [], "f.cs", 1));
        sink.HostReady = true;
        sink.Handle(new StepEvent(DateTime.Now, 1, "CMN-PIP-11", null, "sql", [], "f.cs", 1));

        // Assert
        output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(2);
        sink.KeysSeen.Should().BeEquivalentTo(["USR-SED-01", "CMN-PIP-11"]);
        waiter.Find("USR-SED-01").Should().NotBeNull();
    }
}
