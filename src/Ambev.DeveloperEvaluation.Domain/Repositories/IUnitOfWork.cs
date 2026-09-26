namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TD-006
/// <summary>
/// Opens and closes the explicit database transaction that the repository writes of one command run in.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Begins a transaction that the following repository writes join.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the open transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the open transaction, discarding its writes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
