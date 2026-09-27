#if DEBUG
using System.Globalization;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Diagnostics;

// Work item: TASK-082 (FEAT-019)
/// <summary>
/// Whether the diagnostics trace buffer records StepTrace events, and how many it keeps. Both values are required;
/// Debug builds only.
/// </summary>
public sealed class DiagnosticsSettings
{
    // Work item: TASK-082 (FEAT-019)
    /// <summary>
    /// Configuration key of whether the trace buffer is the StepTrace sink.
    /// </summary>
    public const string TraceEnabledKey = "Diagnostics:Trace:Enabled";

    // Work item: TASK-082 (FEAT-019)
    /// <summary>
    /// Configuration key of how many events the trace buffer keeps.
    /// </summary>
    public const string TraceCapacityKey = "Diagnostics:Trace:Capacity";

    private DiagnosticsSettings(bool traceEnabled, int traceCapacity)
    {
        TraceEnabled = traceEnabled;
        TraceCapacity = traceCapacity;
    }

    // Work item: TASK-082 (FEAT-019)
    /// <summary>
    /// Gets whether the trace buffer records events.
    /// </summary>
    public bool TraceEnabled { get; }

    // Work item: TASK-082 (FEAT-019)
    /// <summary>
    /// Gets how many events the trace buffer keeps.
    /// </summary>
    public int TraceCapacity { get; }

    // Work item: TASK-082 (FEAT-019)
    /// <summary>
    /// Reads both keys, failing with a message that names a missing or invalid key.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The settings</returns>
    public static DiagnosticsSettings FromConfiguration(IConfiguration configuration)
    {
        var enabled = Required(configuration, TraceEnabledKey);
        if (!bool.TryParse(enabled, out var traceEnabled))
            throw new InvalidOperationException($"{TraceEnabledKey} must be true or false.");

        var capacity = Required(configuration, TraceCapacityKey);
        if (!int.TryParse(capacity, NumberStyles.None, CultureInfo.InvariantCulture, out var traceCapacity) || traceCapacity < 1)
            throw new InvalidOperationException($"{TraceCapacityKey} must be a whole number of at least 1.");

        return new DiagnosticsSettings(traceEnabled, traceCapacity);
    }

    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException(
                $"{key} is not configured. Set it in appsettings or via the {key.Replace(":", "__")} environment variable.");
}
#endif
