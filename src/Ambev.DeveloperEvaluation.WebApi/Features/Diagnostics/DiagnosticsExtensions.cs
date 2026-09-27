#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.WebApi.Tracing;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Diagnostics;

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// Registers the Debug-only diagnostics: settings, trace buffer, and outbox reader.
/// </summary>
public static class DiagnosticsExtensions
{
    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Reads the diagnostics settings (a missing key fails startup naming it), registers the buffer and the outbox
    /// reader, and makes the buffer the StepTrace sink when the trace is enabled.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The same builder</returns>
    public static WebApplicationBuilder AddDiagnostics(this WebApplicationBuilder builder)
    {
        var settings = DiagnosticsSettings.FromConfiguration(builder.Configuration);
        var buffer = new TraceBuffer(settings.TraceCapacity);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(buffer);
        builder.Services.AddScoped<OutboxInspector>();
        if (settings.TraceEnabled)
            StepTrace.Sink = buffer.Record;

        return builder;
    }
}
#endif
