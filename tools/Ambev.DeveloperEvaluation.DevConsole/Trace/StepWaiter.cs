using Ambev.DeveloperEvaluation.Common.Tracing;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Remembers every traced event and lets a scenario wait for a key carrying given values, whether the event already
/// happened or is still to come. Worker and relay threads record; the scenario thread waits.
/// </summary>
public sealed class StepWaiter
{
    private readonly object _lock = new();
    private readonly List<StepEvent> _history = new();
    private readonly List<(Func<StepEvent, bool> Match, TaskCompletionSource<StepEvent> Completion)> _pending = new();

    /// <summary>
    /// Records an event and completes any wait it satisfies.
    /// </summary>
    /// <param name="stepEvent">The event</param>
    public void Record(StepEvent stepEvent)
    {
        List<TaskCompletionSource<StepEvent>>? completed = null;
        lock (_lock)
        {
            _history.Add(stepEvent);
            for (var index = _pending.Count - 1; index >= 0; index--)
            {
                if (!_pending[index].Match(stepEvent))
                    continue;

                (completed ??= new()).Add(_pending[index].Completion);
                _pending.RemoveAt(index);
            }
        }

        completed?.ForEach(completion => completion.TrySetResult(stepEvent));
    }

    /// <summary>
    /// Finds the first recorded event with the key (topic or shared) and every given value.
    /// </summary>
    /// <param name="key">The step key</param>
    /// <param name="match">Name and value pairs that must all be present</param>
    /// <returns>The event, or null</returns>
    public StepEvent? Find(string key, params (string Name, string Value)[] match)
    {
        lock (_lock)
        {
            return _history.FirstOrDefault(stepEvent => Matches(stepEvent, key, match));
        }
    }

    /// <summary>
    /// Waits for an event with the key and the values, returning at once if it was already recorded.
    /// </summary>
    /// <param name="key">The step key</param>
    /// <param name="timeout">How long to wait</param>
    /// <param name="match">Name and value pairs that must all be present</param>
    /// <returns>The event, or null on timeout</returns>
    public async Task<StepEvent?> WaitAsync(string key, TimeSpan timeout, params (string Name, string Value)[] match)
    {
        var completion = new TaskCompletionSource<StepEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            var found = _history.FirstOrDefault(stepEvent => Matches(stepEvent, key, match));
            if (found is not null)
                return found;

            _pending.Add((stepEvent => Matches(stepEvent, key, match), completion));
        }

        try
        {
            return await completion.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            lock (_lock)
            {
                _pending.RemoveAll(pending => pending.Completion == completion);

                // An event recorded between the timeout and the removal completed nothing the caller still awaits.
                return _history.FirstOrDefault(stepEvent => Matches(stepEvent, key, match));
            }
        }
    }

    private static bool Matches(StepEvent stepEvent, string key, (string Name, string Value)[] match) =>
        (stepEvent.Key == key || stepEvent.SharedKey == key)
        && match.All(pair => stepEvent.Values.Contains(pair));
}
