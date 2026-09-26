using Ambev.DeveloperEvaluation.ORM.Outbox;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TD-030
/// <summary>
/// Wraps the outbox relay in the trace host so a scenario can make one cycle fail (SAL-RLY-07).
/// </summary>
public sealed class FaultyOutboxRelay : IOutboxRelay
{
    private readonly IOutboxRelay _inner;
    private readonly TraceFaults _faults;

    /// <summary>
    /// Initializes a new instance of FaultyOutboxRelay.
    /// </summary>
    /// <param name="inner">The relay that runs when no fault is armed</param>
    /// <param name="faults">The armed faults</param>
    public FaultyOutboxRelay(IOutboxRelay inner, TraceFaults faults)
    {
        _inner = inner;
        _faults = faults;
    }

    /// <inheritdoc />
    public Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken) =>
        _faults.TakeRelayCycle()
            ? throw new InvalidOperationException("Relay cycle failure injected by the trace console.")
            : _inner.DispatchPendingAsync(batchSize, cancellationToken);
}
