#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-081 (FEAT-019)
/// <summary>
/// One traced step kept by the <see cref="TraceBuffer"/>, with the cursor it was stored under.
/// </summary>
/// <param name="Cursor">The position of the event; the first event is 1</param>
/// <param name="Event">The traced step</param>
public sealed record BufferedStepEvent(long Cursor, StepEvent Event);

// Work item: TASK-081 (FEAT-019)
/// <summary>
/// Keeps the latest StepTrace events in memory so the diagnostics route can serve them after a cursor. When full,
/// the oldest event is overwritten. Events traced while <see cref="Muted"/> is true in the current async flow are
/// ignored, so the diagnostics requests never record themselves. Debug builds only.
/// </summary>
public sealed class TraceBuffer
{
    private static readonly AsyncLocal<bool> MutedFlow = new();

    private readonly object _lock = new();
    private readonly BufferedStepEvent?[] _slots;
    private long _head;

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Initializes a new instance of TraceBuffer
    /// </summary>
    /// <param name="capacity">How many events the buffer keeps; at least 1</param>
    public TraceBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _slots = new BufferedStepEvent?[capacity];
    }

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Gets how many events the buffer keeps.
    /// </summary>
    public int Capacity => _slots.Length;

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Gets the cursor of the last event recorded, or 0 before any event.
    /// </summary>
    public long Head
    {
        get
        {
            lock (_lock)
            {
                return _head;
            }
        }
    }

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Gets or sets whether events traced in the current async flow are ignored. A value set inside an async method
    /// flows to what that method awaits and is gone when it returns.
    /// </summary>
    public static bool Muted
    {
        get => MutedFlow.Value;
        set => MutedFlow.Value = value;
    }

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Stores one event under the next cursor, unless the current flow is muted. This is the StepTrace sink.
    /// </summary>
    /// <param name="stepEvent">The traced step</param>
    public void Record(StepEvent stepEvent)
    {
        if (MutedFlow.Value)
            return;

        lock (_lock)
        {
            _head++;
            _slots[(_head - 1) % _slots.Length] = new BufferedStepEvent(_head, stepEvent);
        }
    }

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Returns the retained events with a cursor greater than the given one, in cursor order. A cursor older than the
    /// oldest retained event returns every retained event.
    /// </summary>
    /// <param name="cursor">The last cursor the caller has seen; 0 for none</param>
    /// <returns>The later events</returns>
    public IReadOnlyList<BufferedStepEvent> ReadAfter(long cursor)
    {
        lock (_lock)
        {
            if (cursor >= _head)
                return [];

            var oldest = Math.Max(1, _head - _slots.Length + 1);
            var events = new List<BufferedStepEvent>();
            for (var next = Math.Max(cursor + 1, oldest); next <= _head; next++)
                events.Add(_slots[(next - 1) % _slots.Length]!);

            return events;
        }
    }
}
#endif
