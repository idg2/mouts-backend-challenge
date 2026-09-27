#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Tracing;

// Work item: TASK-081 (FEAT-019)
/// <summary>
/// Contains unit tests for the <see cref="TraceBuffer"/> class.
/// </summary>
public class TraceBufferTests
{
    // Work item: TASK-081 (FEAT-019)
    [Fact(DisplayName = "Given an empty buffer When reading Then the head is zero and nothing is returned")]
    public void Given_EmptyBuffer_When_Reading_Then_HeadZeroAndNothingReturned()
    {
        // Arrange
        var buffer = new TraceBuffer(3);

        // Act
        var events = buffer.ReadAfter(0);

        // Assert
        buffer.Head.Should().Be(0);
        events.Should().BeEmpty();
    }

    // Work item: TASK-081 (FEAT-019)
    [Fact(DisplayName = "Given recorded events When reading after a cursor Then only later events return in cursor order")]
    public void Given_RecordedEvents_When_ReadingAfterCursor_Then_OnlyLaterEventsInOrder()
    {
        // Arrange
        var buffer = new TraceBuffer(10);
        buffer.Record(Event("SAL-CRT-01"));
        buffer.Record(Event("SAL-CRT-02"));
        buffer.Record(Event("SAL-CRT-03"));

        // Act
        var events = buffer.ReadAfter(1);

        // Assert
        buffer.Head.Should().Be(3);
        events.Select(e => e.Cursor).Should().Equal(2, 3);
        events.Select(e => e.Event.Key).Should().Equal("SAL-CRT-02", "SAL-CRT-03");
    }

    // Work item: TASK-081 (FEAT-019)
    [Theory(DisplayName = "Given a wrapped buffer When reading after an old cursor Then only the retained events return")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Given_WrappedBuffer_When_ReadingAfterOldCursor_Then_RetainedEventsReturn(long after)
    {
        // Arrange
        var buffer = new TraceBuffer(3);
        for (var index = 1; index <= 5; index++)
            buffer.Record(Event($"SAL-CRT-0{index}"));

        // Act
        var events = buffer.ReadAfter(after);

        // Assert
        events.Select(e => e.Cursor).Should().Equal(3, 4, 5);
        events.Select(e => e.Event.Key).Should().Equal("SAL-CRT-03", "SAL-CRT-04", "SAL-CRT-05");
    }

    // Work item: TASK-081 (FEAT-019)
    [Fact(DisplayName = "Given a cursor at the head When reading Then nothing is returned")]
    public void Given_CursorAtHead_When_Reading_Then_NothingReturned()
    {
        // Arrange
        var buffer = new TraceBuffer(3);
        buffer.Record(Event("SAL-CRT-01"));

        // Act
        var events = buffer.ReadAfter(buffer.Head);

        // Assert
        events.Should().BeEmpty();
    }

    // Work item: TASK-081 (FEAT-019)
    [Fact(DisplayName = "Given the largest cursor When reading Then nothing is returned")]
    public void Given_LargestCursor_When_Reading_Then_NothingReturned()
    {
        // Arrange
        var buffer = new TraceBuffer(3);
        buffer.Record(Event("SAL-CRT-01"));

        // Act
        var events = buffer.ReadAfter(long.MaxValue);

        // Assert
        events.Should().BeEmpty();
    }

    // Work item: TASK-081 (FEAT-019)
    [Fact(DisplayName = "Given a muted flow When recording Then the event is ignored and other flows still record")]
    public async Task Given_MutedFlow_When_Recording_Then_IgnoredAndOtherFlowsRecord()
    {
        // Arrange
        var buffer = new TraceBuffer(3);

        // Act
        await Task.Run(() =>
        {
            TraceBuffer.Muted = true;
            buffer.Record(Event("CMN-PIP-01"));
        });
        buffer.Record(Event("SAL-CRT-01"));

        // Assert
        TraceBuffer.Muted.Should().BeFalse();
        buffer.ReadAfter(0).Select(e => e.Event.Key).Should().Equal("SAL-CRT-01");
    }

    // Work item: TASK-081 (FEAT-019)
    [Theory(DisplayName = "Given a capacity below one When creating the buffer Then it throws")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_CapacityBelowOne_When_Creating_Then_Throws(int capacity)
    {
        // Act
        var create = () => new TraceBuffer(capacity);

        // Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static StepEvent Event(string key) =>
        new(DateTime.Now, 1, key, null, "title", Array.Empty<(string Name, string Value)>(), "/src/File.cs", 10);
}
#endif
