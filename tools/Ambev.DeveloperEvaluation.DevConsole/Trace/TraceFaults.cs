namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TD-030
/// <summary>
/// The faults a scenario can arm in the API hosted by <see cref="TraceHost"/>. Each one fires once, on the next relay
/// cycle or the next controller action, and then disarms itself.
/// </summary>
public sealed class TraceFaults
{
    private int _relayCycle;
    private int _request;

    /// <summary>
    /// Makes the next outbox relay cycle throw, before it reads any row.
    /// </summary>
    public void FailNextRelayCycle() => Interlocked.Exchange(ref _relayCycle, 1);

    /// <summary>
    /// Makes the next controller action throw an exception no middleware expects.
    /// </summary>
    public void FailNextRequest() => Interlocked.Exchange(ref _request, 1);

    /// <summary>
    /// Returns whether the relay fault is armed, and disarms it.
    /// </summary>
    public bool TakeRelayCycle() => Interlocked.Exchange(ref _relayCycle, 0) == 1;

    /// <summary>
    /// Returns whether the request fault is armed, and disarms it.
    /// </summary>
    public bool TakeRequest() => Interlocked.Exchange(ref _request, 0) == 1;
}
