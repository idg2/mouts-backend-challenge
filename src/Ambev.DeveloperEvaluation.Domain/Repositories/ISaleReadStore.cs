using Ambev.DeveloperEvaluation.Domain.Events.Sales;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-075 (FEAT-003)
/// <summary>
/// The sale read model: the projection the sale events keep and the get and list operations read. Writes are
/// ordered by the outbox sequence of the event, so an older event never overwrites a newer state.
/// </summary>
public interface ISaleReadStore
{
    /// <summary>
    /// Stores the sale state carried by SaleCreated or SaleModified when its sequence is newer than the stored one.
    /// </summary>
    /// <param name="snapshot">The sale state</param>
    /// <param name="version">The outbox sequence of the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True when applied, false when the stored state is newer or equal</returns>
    Task<bool> UpsertAsync(SaleSnapshot snapshot, long version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the sale deleted (a tombstone) when the sequence is newer than the stored one; the tombstone hides the
    /// sale from reads and outranks older events that arrive later.
    /// </summary>
    /// <param name="saleId">The sale id</param>
    /// <param name="version">The outbox sequence of the SaleDeleted event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True when applied, false when the stored state is newer or equal</returns>
    Task<bool> MarkDeletedAsync(Guid saleId, long version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one sale with its items in line order.
    /// </summary>
    /// <param name="saleId">The sale id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sale, or null when absent or deleted</returns>
    Task<SaleSnapshot?> GetAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one page of sales that match the query's filters, in the query's order or else by sale number.
    /// </summary>
    /// <param name="query">The page, size, filters, and sort fields, on the response property names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sales on the page and the number of sales that match the filters</returns>
    Task<(IReadOnlyList<SaleSnapshot> Items, int TotalCount)> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
}
