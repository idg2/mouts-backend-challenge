namespace Ambev.DeveloperEvaluation.ORM.Outbox;

// Work item: TASK-031 (FEAT-004)
/// <summary>
/// One dispatch cycle of the outbox, as the relay service runs it.
/// </summary>
public interface IOutboxRelay
{
    /// <summary>
    /// Publishes up to <paramref name="batchSize"/> pending rows, oldest first.
    /// </summary>
    /// <param name="batchSize">The maximum number of rows read in this cycle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of rows published and marked processed</returns>
    Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken);
}
