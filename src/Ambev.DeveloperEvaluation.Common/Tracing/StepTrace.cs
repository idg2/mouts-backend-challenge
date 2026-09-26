using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Ambev.DeveloperEvaluation.Common.Tracing;

// Work item: TASK-042 (FEAT-017), TD-038
/// <summary>
/// Prints one line per step key documented in docs. Every Step call is compiled away in Release, together
/// with its arguments, and in Debug nothing happens until a sink is installed, so only the trace console sees lines.
/// </summary>
public static class StepTrace
{
    /// <summary>
    /// Gets or sets the sink that receives every event. Null, the default, disables the trace.
    /// </summary>
    public static Action<StepEvent>? Sink { get; set; }

    /// <summary>
    /// Traces one documented step.
    /// </summary>
    /// <param name="key">The step key, exactly as documented</param>
    /// <param name="title">The diagram label without the key</param>
    /// <param name="values">The values that decided the path, as name and value pairs</param>
    /// <param name="file">Filled by the compiler</param>
    /// <param name="line">Filled by the compiler</param>
    [Conditional("DEBUG")]
    public static void Step(
        string key,
        string title,
        (string Name, object? Value)[]? values = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
#if DEBUG
        Emit(key, null, title, values, file, line);
#endif
    }

    /// <summary>
    /// Traces one documented step that is also a shared CMN step, on one line with both keys.
    /// </summary>
    /// <param name="key">The topic step key</param>
    /// <param name="sharedKey">The CMN step key the same line also documents</param>
    /// <param name="title">The diagram label without the key</param>
    /// <param name="values">The values that decided the path</param>
    /// <param name="file">Filled by the compiler</param>
    /// <param name="line">Filled by the compiler</param>
    [Conditional("DEBUG")]
    public static void Step(
        string key,
        string sharedKey,
        string title,
        (string Name, object? Value)[]? values = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
#if DEBUG
        Emit(key, sharedKey, title, values, file, line);
#endif
    }

    /// <summary>
    /// Traces a shared line of generic code: the CMN key is given, and the topic key comes from
    /// <see cref="StepKeys"/> by the request type name.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type being handled</typeparam>
    /// <param name="point">The shared point</param>
    /// <param name="sharedKey">The CMN step key</param>
    /// <param name="title">The diagram label without the key</param>
    /// <param name="values">The values that decided the path</param>
    /// <param name="file">Filled by the compiler</param>
    /// <param name="line">Filled by the compiler</param>
    [Conditional("DEBUG")]
    public static void Step<TRequest>(
        SharedPoint point,
        string sharedKey,
        string title,
        (string Name, object? Value)[]? values = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
#if DEBUG
        Emit(StepKeys.Resolve(typeof(TRequest).Name, point), sharedKey, title, values, file, line);
#endif
    }

#if DEBUG
    // Counts Format calls; read by the unit tests to prove a null sink formats nothing.
    internal static int FormatCount;

    // Work item: TASK-042 (FEAT-017), TASK-059 (FEAT-017)
    private static void Emit(
        string? key, string? sharedKey, string title, (string Name, object? Value)[]? values, string file, int line)
    {
        var sink = Sink;
        if (sink is null)
            return;

        try
        {
            var formatted = values is null
                ? Array.Empty<(string Name, string Value)>()
                : values.Select(value => (value.Name, Format(value.Value))).ToArray();
            sink(new StepEvent(DateTime.Now, Environment.CurrentManagedThreadId, key, sharedKey, title, formatted, file, line));
        }
        catch
        {
            // A broken sink or a throwing ToString never breaks the API; the event is dropped.
        }
    }

    // Work item: TASK-042 (FEAT-017), TASK-059 (FEAT-017)
    // Line breaks become spaces so every event stays on one line; an enumerable is never enumerated, so a lazy
    // sequence or an IQueryable cannot run.
    internal static string Format(object? value)
    {
        Interlocked.Increment(ref FormatCount);
        return value switch
        {
            null => "null",
            string text => text.ReplaceLineEndings(" "),
            Guid guid => guid.ToString("D"),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            Exception exception => $"{exception.GetType().Name}: {exception.Message}".ReplaceLineEndings(" "),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            ICollection collection => collection.Count.ToString(CultureInfo.InvariantCulture),
            IEnumerable => value.GetType().Name,
            _ => value.ToString() ?? "null"
        };
    }
#else
    // Release: the property type must exist, so StepEvent and SharedPoint stay; nothing else does.
    internal static int FormatCount => 0;
#endif
}
