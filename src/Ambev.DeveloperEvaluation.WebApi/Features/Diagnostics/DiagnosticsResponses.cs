#if DEBUG
using System.Text.Json;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.WebApi.Tracing;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Diagnostics;

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// The anonymous diagnostics status. A 200 itself means a Debug build.
/// </summary>
/// <param name="TraceEnabled">Whether the trace buffer records events</param>
public sealed record DiagnosticsStatusResponse(bool TraceEnabled);

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// Outbox rows after a sequence.
/// </summary>
/// <param name="Head">The highest outbox sequence, or 0</param>
/// <param name="Items">The rows after the requested sequence; empty when none was requested</param>
public sealed record OutboxPageResponse(long Head, IReadOnlyList<OutboxEntryResponse> Items);

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// One outbox row, with its payload as JSON.
/// </summary>
/// <param name="Id">The event id</param>
/// <param name="Sequence">The dispatch order</param>
/// <param name="Type">The event type name</param>
/// <param name="OccurredAt">When the event was recorded</param>
/// <param name="ProcessedAt">When the relay sent it, or null while pending</param>
/// <param name="Payload">The stored event, as written (PascalCase members)</param>
public sealed record OutboxEntryResponse(
    Guid Id, long Sequence, string Type, DateTime OccurredAt, DateTime? ProcessedAt, JsonElement Payload)
{
    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Maps a stored row.
    /// </summary>
    /// <param name="message">The outbox row</param>
    /// <returns>The response entry</returns>
    public static OutboxEntryResponse From(OutboxMessage message)
    {
        using var payload = JsonDocument.Parse(message.Payload);
        return new OutboxEntryResponse(
            message.Id, message.Sequence, message.Type, message.OccurredAt, message.ProcessedAt, payload.RootElement.Clone());
    }
}

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// Trace events after a cursor.
/// </summary>
/// <param name="Head">The last cursor issued, or 0</param>
/// <param name="Items">The events after the requested cursor; empty when none was requested</param>
public sealed record TracePageResponse(long Head, IReadOnlyList<TraceEntryResponse> Items);

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// One traced step.
/// </summary>
/// <param name="Cursor">The position of the event</param>
/// <param name="At">The local time of the call</param>
/// <param name="ThreadId">The managed thread id of the caller</param>
/// <param name="Key">The topic step key, or null</param>
/// <param name="SharedKey">The CMN step key the same line documents, or null</param>
/// <param name="Title">The diagram label without the key</param>
/// <param name="Values">The values that decided the path</param>
/// <param name="File">The source file name, without its directory</param>
/// <param name="Line">The source line</param>
public sealed record TraceEntryResponse(
    long Cursor, DateTime At, int ThreadId, string? Key, string? SharedKey, string Title,
    IReadOnlyList<TraceValueResponse> Values, string File, int Line)
{
    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Maps a buffered event.
    /// </summary>
    /// <param name="buffered">The buffered event</param>
    /// <returns>The response entry</returns>
    public static TraceEntryResponse From(BufferedStepEvent buffered)
    {
        var stepEvent = buffered.Event;
        return new TraceEntryResponse(
            buffered.Cursor, stepEvent.At, stepEvent.ThreadId, stepEvent.Key, stepEvent.SharedKey, stepEvent.Title,
            stepEvent.Values.Select(value => new TraceValueResponse(value.Name, value.Value)).ToList(),
            stepEvent.FileName, stepEvent.Line);
    }
}

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// One formatted name and value of a traced step.
/// </summary>
/// <param name="Name">The value name</param>
/// <param name="Value">The formatted value</param>
public sealed record TraceValueResponse(string Name, string Value);
#endif
