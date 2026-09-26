using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// The StepTrace sink of the console: prints one line per event, records the keys seen, and feeds the waiter.
/// Until the host is ready it drops CMN-PIP-11, so the migration statements do not flood the start of a run.
/// </summary>
public sealed class ConsoleSink
{
    private readonly TextWriter _output;
    private readonly StepWaiter _waiter;
    private readonly object _lock = new();
    private readonly HashSet<string> _keysSeen = new();

    /// <summary>
    /// Initializes a new instance of ConsoleSink
    /// </summary>
    /// <param name="output">Where the lines go</param>
    /// <param name="waiter">The waiter that scenarios use</param>
    public ConsoleSink(TextWriter output, StepWaiter waiter)
    {
        _output = output;
        _waiter = waiter;
    }

    /// <summary>
    /// Gets or sets whether the host has been built. False drops CMN-PIP-11 lines.
    /// </summary>
    public bool HostReady { get; set; }

    /// <summary>
    /// Gets the distinct keys printed so far, topic and shared keys alike.
    /// </summary>
    public IReadOnlyCollection<string> KeysSeen
    {
        get
        {
            lock (_lock)
            {
                return _keysSeen.ToList();
            }
        }
    }

    /// <summary>
    /// Handles one event: print, record the keys, and feed the waiter.
    /// </summary>
    /// <param name="stepEvent">The event</param>
    public void Handle(StepEvent stepEvent)
    {
        if (!HostReady && stepEvent.Key == "CMN-PIP-11")
            return;

        lock (_lock)
        {
            _output.WriteLine(Format(stepEvent));
            if (stepEvent.Key is not null)
                _keysSeen.Add(stepEvent.Key);
            if (stepEvent.SharedKey is not null)
                _keysSeen.Add(stepEvent.SharedKey);
        }

        _waiter.Record(stepEvent);
    }

    /// <summary>
    /// Formats one line: time with microseconds, thread, keys, title, values, and the source file and line.
    /// </summary>
    /// <param name="stepEvent">The event</param>
    /// <returns>The line</returns>
    public static string Format(StepEvent stepEvent)
    {
        var values = string.Join(' ', stepEvent.Values.Select(value => $"{value.Name}={value.Value}"));
        return string.Join("  ",
            stepEvent.At.ToString("HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            $"T{stepEvent.ThreadId:D3}",
            stepEvent.Keys,
            stepEvent.Title,
            values,
            $"{stepEvent.FileName}:{stepEvent.Line}");
    }
}
