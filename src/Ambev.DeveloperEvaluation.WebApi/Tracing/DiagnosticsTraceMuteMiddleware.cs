#if DEBUG
namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-081 (FEAT-019)
/// <summary>
/// Mutes the <see cref="TraceBuffer"/> for requests to the diagnostics routes, so reading the trace never records
/// the read itself. Registered first in the pipeline, before any traced middleware. Debug builds only.
/// </summary>
public sealed class DiagnosticsTraceMuteMiddleware
{
    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// The path prefix of the diagnostics routes.
    /// </summary>
    public const string PathPrefix = "/api/diagnostics";

    private readonly RequestDelegate _next;

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Initializes a new instance of DiagnosticsTraceMuteMiddleware
    /// </summary>
    /// <param name="next">The rest of the pipeline</param>
    public DiagnosticsTraceMuteMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Work item: TASK-081 (FEAT-019)
    /// <summary>
    /// Sets the mute flag for a diagnostics request, then runs the rest of the pipeline. The method is async on
    /// purpose: the flag then flows into the pipeline and is restored when the method returns.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(PathPrefix, StringComparison.OrdinalIgnoreCase))
            TraceBuffer.Muted = true;

        await _next(context);
    }
}
#endif
