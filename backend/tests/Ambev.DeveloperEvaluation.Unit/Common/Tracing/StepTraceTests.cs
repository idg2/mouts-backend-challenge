using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Ambev.DeveloperEvaluation.Common.Tracing;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Tracing;

// Work item: TASK-042 (FEAT-017)
/// <summary>
/// Contains unit tests for the <see cref="StepTrace"/> class. The tests set and clear the static sink, so they are
/// not parallelized with each other.
/// </summary>
[Collection("StepTrace")]
public class StepTraceTests : IDisposable
{
    private readonly List<StepEvent> _events = new();

    public StepTraceTests()
    {
        StepTrace.Sink = _events.Add;
    }

    public void Dispose()
    {
        StepTrace.Sink = null;
    }

    [Fact(DisplayName = "Given no sink When stepping Then no event and no formatting happen")]
    public void Given_NoSink_When_Step_Then_NothingIsFormatted()
    {
        // Arrange
        StepTrace.Sink = null;
        var evaluated = false;
        object? Value() { evaluated = true; return 1; }
        var before = StepTrace.FormatCount;

        // Act
        StepTrace.Step("SAL-CRT-01", "Validate the request", [("valid", Value())]);

        // Assert: the caller builds the value array, so Value() runs in Debug; the helper must not format it.
        evaluated.Should().BeTrue();
        _events.Should().BeEmpty();
        (StepTrace.FormatCount - before).Should().Be(0);
    }

    [Fact(DisplayName = "Given a sink When stepping Then the event carries key, title, thread, file, line, and values")]
    public void Given_Sink_When_Step_Then_EventCarriesEverything()
    {
        // Arrange
        var id = Guid.Parse("0b6f4b1e-1c2d-4e5f-8a9b-0c1d2e3f4a5b");

        // Act
        StepTrace.Step("SAL-CRT-05", "Preset id already stored?", [("id", id), ("stored", false)]);

        // Assert
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().Be("SAL-CRT-05");
        stepEvent.SharedKey.Should().BeNull();
        stepEvent.Keys.Should().Be("SAL-CRT-05");
        stepEvent.Title.Should().Be("Preset id already stored?");
        stepEvent.ThreadId.Should().Be(Environment.CurrentManagedThreadId);
        stepEvent.FileName.Should().Be("StepTraceTests.cs");
        stepEvent.Line.Should().BeGreaterThan(0);
        stepEvent.Values.Should().Equal(("id", "0b6f4b1e-1c2d-4e5f-8a9b-0c1d2e3f4a5b"), ("stored", "False"));
    }

    [Fact(DisplayName = "Given a shared key When stepping Then both keys are on the event")]
    public void Given_SharedKey_When_Step_Then_BothKeysPresent()
    {
        // Act
        StepTrace.Step("SAL-CRT-01", "CMN-PIP-04", "Validate the request", [("valid", true)]);

        // Assert
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().Be("SAL-CRT-01");
        stepEvent.SharedKey.Should().Be("CMN-PIP-04");
        stepEvent.Keys.Should().Be("SAL-CRT-01 CMN-PIP-04");
    }

    [Fact(DisplayName = "Given a request type in the table When stepping a shared point Then the topic key is resolved")]
    public void Given_KnownRequest_When_StepShared_Then_TopicKeyResolved()
    {
        // Act
        StepTrace.Step<CreateSaleCommand>(SharedPoint.TransactionCommit, "CMN-TXN-06", "Commit");

        // Assert
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().Be("SAL-CRT-12");
        stepEvent.SharedKey.Should().Be("CMN-TXN-06");
    }

    [Fact(DisplayName = "Given a request type without a key for the point When stepping Then only the shared key prints")]
    public void Given_KnownRequestWithoutPointKey_When_StepShared_Then_SharedKeyOnly()
    {
        // Act
        StepTrace.Step<CreateCustomerCommand>(SharedPoint.TransactionCommit, "CMN-TXN-06", "Commit");

        // Assert
        var stepEvent = _events.Should().ContainSingle().Subject;
        stepEvent.Key.Should().BeNull();
        stepEvent.Keys.Should().Be("CMN-TXN-06");
    }

    [Fact(DisplayName = "Given an unknown request type When stepping a shared point Then the topic is ???")]
    public void Given_UnknownRequest_When_StepShared_Then_UnknownTopic()
    {
        // Act
        StepTrace.Step<StepTraceTests>(SharedPoint.TransactionBegin, "CMN-TXN-03", "Begin the transaction");

        // Assert
        _events.Should().ContainSingle().Which.Key.Should().Be("???");
    }

    [Theory(DisplayName = "Given a value When formatting Then invariant culture and the documented shapes are used")]
    [InlineData(null, "null")]
    [InlineData("text", "text")]
    [InlineData(1234.5, "1234.5")]
    [InlineData(true, "True")]
    public void Given_Value_When_Formatting_Then_DocumentedShape(object? value, string expected)
    {
        // Arrange
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            // Act
            StepTrace.Step("SAL-CRT-01", "t", [("v", value)]);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        // Assert
        _events.Single().Values.Single().Value.Should().Be(expected);
    }

    [Fact(DisplayName = "Given dates, collections, and exceptions When formatting Then ISO, count, and type with message")]
    public void Given_SpecialValues_When_Formatting_Then_DocumentedShape()
    {
        // Arrange
        var date = new DateTime(2026, 9, 25, 14, 3, 21, DateTimeKind.Utc).AddTicks(4829130);
        var offset = new DateTimeOffset(2026, 9, 25, 14, 3, 21, TimeSpan.FromHours(-3));
        var list = new List<int> { 1, 2, 3 };
        var exception = new InvalidOperationException("boom");

        // Act
        StepTrace.Step("SAL-CRT-01", "t", [("d", date), ("o", offset), ("l", list), ("e", exception), ("m", 12.30m)]);

        // Assert
        _events.Single().Values.Should().Equal(
            ("d", "2026-09-25T14:03:21.4829130Z"),
            ("o", "2026-09-25T14:03:21.0000000-03:00"),
            ("l", "3"),
            ("e", "InvalidOperationException: boom"),
            ("m", "12.30"));
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given multi-line text and exception messages When formatting Then each value stays on one line")]
    public void Given_MultiLineValues_When_Formatting_Then_OneLine()
    {
        // Arrange
        var exception = new InvalidOperationException("Validation failed: \r\n -- Items[0]: not found\nlast");

        // Act
        StepTrace.Step("SAL-CRT-01", "t", [("s", "first\r\nsecond\nthird\rfourth"), ("e", exception)]);

        // Assert
        _events.Single().Values.Should().Equal(
            ("s", "first second third fourth"),
            ("e", "InvalidOperationException: Validation failed:   -- Items[0]: not found last"));
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given a collection and a lazy sequence When formatting Then the count and the type name print, without enumerating")]
    public void Given_CollectionAndLazySequence_When_Formatting_Then_CountAndTypeNameWithoutEnumerating()
    {
        // Arrange
        var enumerated = false;
        var list = new List<int> { 1, 2, 3 };
        var sequence = Enumerable.Range(0, 3).Select(x => { enumerated = true; return x; });

        // Act
        StepTrace.Step("SAL-CRT-01", "t", [("l", list), ("q", sequence)]);

        // Assert
        _events.Single().Values.Should().Equal(("l", "3"), ("q", sequence.GetType().Name));
        enumerated.Should().BeFalse();
    }

    // Work item: TASK-059 (FEAT-017)
    [Fact(DisplayName = "Given a value whose ToString throws When stepping Then the exception is swallowed and the event dropped")]
    public void Given_ThrowingToString_When_Step_Then_SwallowedAndDropped()
    {
        // Act
        var act = () => StepTrace.Step("SAL-CRT-01", "t", [("v", new ThrowingToString())]);

        // Assert
        act.Should().NotThrow();
        _events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Given a throwing sink When stepping Then the exception is swallowed")]
    public void Given_ThrowingSink_When_Step_Then_ExceptionIsSwallowed()
    {
        // Arrange
        StepTrace.Sink = _ => throw new IOException("closed");

        // Act
        var act = () => StepTrace.Step("SAL-CRT-01", "t");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Given the helper When inspecting Then every public Step overload is conditional on DEBUG")]
    public void Given_Helper_When_Inspecting_Then_EveryStepIsConditionalOnDebug()
    {
        // Act
        var steps = typeof(StepTrace).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(StepTrace.Step))
            .ToList();

        // Assert
        steps.Should().HaveCount(3);
        steps.Should().OnlyContain(method =>
            method.GetCustomAttributes<ConditionalAttribute>().Any(attribute => attribute.ConditionString == "DEBUG"));
    }

    // Nested fakes: StepKeys is keyed by the simple type name, so these resolve like the real commands.
    private sealed class CreateSaleCommand { }

    private sealed class CreateCustomerCommand { }

    // Work item: TASK-059 (FEAT-017)
    private sealed class ThrowingToString
    {
        public override string ToString() => throw new InvalidOperationException("broken ToString");
    }
}
