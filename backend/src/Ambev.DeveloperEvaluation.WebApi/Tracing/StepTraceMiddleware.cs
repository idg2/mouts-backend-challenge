#if DEBUG
using System.Diagnostics;
using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-046 (FEAT-017)
/// <summary>
/// Traces CMN-PIP-01 when a request enters and CMN-PIP-13 when it leaves, with the status and the elapsed time.
/// Registered right after the Serilog request logging. Debug builds only.
/// </summary>
public class StepTraceMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of StepTraceMiddleware
    /// </summary>
    /// <param name="next">The next middleware</param>
    public StepTraceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Traces the request around the rest of the pipeline.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        StepTrace.Step("CMN-PIP-01", "Request logging starts",
            [("method", context.Request.Method), ("path", context.Request.Path.Value), ("requestId", context.TraceIdentifier)]);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
            StepTrace.Step("CMN-PIP-13", "Request log line with status and elapsed time",
                [("method", context.Request.Method), ("path", context.Request.Path.Value), ("status", context.Response.StatusCode), ("elapsedMs", stopwatch.Elapsed.TotalMilliseconds)]);
        }
        catch
        {
            // No exception value: its type already prints on CMN-RSP-05 (handled=false), and the table lists only
            // method, path, status, and elapsedMs.
            StepTrace.Step("CMN-PIP-13", "Request log line with status and elapsed time",
                [("method", context.Request.Method), ("path", context.Request.Path.Value), ("status", 500), ("elapsedMs", stopwatch.Elapsed.TotalMilliseconds)]);
            throw;
        }
    }
}
#endif
