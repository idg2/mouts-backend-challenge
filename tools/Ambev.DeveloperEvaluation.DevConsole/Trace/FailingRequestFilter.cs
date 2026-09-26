using Microsoft.AspNetCore.Mvc.Filters;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TD-030
/// <summary>
/// An action filter in the trace host that throws once when a scenario arms it, so the request ends in the unhandled
/// exception path (CMN-RSP-10).
/// </summary>
public sealed class FailingRequestFilter : IActionFilter
{
    private readonly TraceFaults _faults;

    /// <summary>
    /// Initializes a new instance of FailingRequestFilter.
    /// </summary>
    /// <param name="faults">The armed faults</param>
    public FailingRequestFilter(TraceFaults faults)
    {
        _faults = faults;
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (_faults.TakeRequest())
            throw new InvalidOperationException("Request failure injected by the trace console.");
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
