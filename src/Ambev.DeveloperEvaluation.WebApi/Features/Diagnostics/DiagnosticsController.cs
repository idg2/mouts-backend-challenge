#if DEBUG
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Diagnostics;

// Work item: TASK-084 (FEAT-019)
/// <summary>
/// Read-only diagnostics for the guided validation UI: the build status, outbox rows after a sequence, and trace
/// events after a cursor. It reads directly, without MediatR: these are not business use cases, and the pipeline
/// would trace and wrap the diagnostic read itself. Compiled only in Debug; Release answers 404 on these routes.
/// </summary>
[ApiController]
[Route("api/diagnostics")]
[Authorize(Roles = AdminRole)]
public class DiagnosticsController : BaseController
{
    private const string AdminRole = "Admin";

    private readonly DiagnosticsSettings _settings;
    private readonly TraceBuffer _buffer;
    private readonly OutboxInspector _inspector;

    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Initializes a new instance of DiagnosticsController
    /// </summary>
    /// <param name="settings">The diagnostics settings</param>
    /// <param name="buffer">The trace buffer</param>
    /// <param name="inspector">The outbox reader</param>
    public DiagnosticsController(DiagnosticsSettings settings, TraceBuffer buffer, OutboxInspector inspector)
    {
        _settings = settings;
        _buffer = buffer;
        _inspector = inspector;
    }

    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Returns whether the trace buffer records events. Anonymous, so a client can tell a Debug API before logging in.
    /// </summary>
    /// <returns>The status</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseWithData<DiagnosticsStatusResponse>), StatusCodes.Status200OK)]
    public IActionResult GetStatus() =>
        Ok(new ApiResponseWithData<DiagnosticsStatusResponse>
        {
            Data = new DiagnosticsStatusResponse(_settings.TraceEnabled),
            Success = true
        });

    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Returns the outbox head and, when a sequence is given, up to 500 rows after it in sequence order.
    /// </summary>
    /// <param name="after">The last sequence the caller has seen; omit it to read only the head</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The head and the rows</returns>
    [HttpGet("outbox")]
    [ProducesResponseType(typeof(ApiResponseWithData<OutboxPageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOutbox([FromQuery] long? after, CancellationToken cancellationToken)
    {
        if (after < 0)
            return BadRequest("after must be zero or greater.");

        var head = await _inspector.HeadAsync(cancellationToken);
        IReadOnlyList<OutboxEntryResponse> items = after is null
            ? []
            : (await _inspector.ReadAfterAsync(after.Value, cancellationToken)).Select(OutboxEntryResponse.From).ToList();
        return Ok(new ApiResponseWithData<OutboxPageResponse> { Data = new OutboxPageResponse(head, items), Success = true });
    }

    // Work item: TASK-084 (FEAT-019)
    /// <summary>
    /// Returns the trace head and, when a cursor is given, the retained events after it in cursor order. With the
    /// trace disabled the head is 0 and there are no events.
    /// </summary>
    /// <param name="after">The last cursor the caller has seen; omit it to read only the head</param>
    /// <returns>The head and the events</returns>
    [HttpGet("trace")]
    [ProducesResponseType(typeof(ApiResponseWithData<TracePageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetTrace([FromQuery] long? after)
    {
        if (after < 0)
            return BadRequest("after must be zero or greater.");

        var page = !_settings.TraceEnabled
            ? new TracePageResponse(0, [])
            : new TracePageResponse(
                _buffer.Head,
                after is null ? [] : _buffer.ReadAfter(after.Value).Select(TraceEntryResponse.From).ToList());
        return Ok(new ApiResponseWithData<TracePageResponse> { Data = page, Success = true });
    }
}
#endif
