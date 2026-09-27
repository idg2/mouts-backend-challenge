#if DEBUG
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Outbox;

// Work item: TASK-083 (FEAT-019)
/// <summary>
/// Reads the outbox table for the diagnostics route: the rows after a sequence and the highest sequence. Read-only,
/// without tracking. Debug builds only.
/// </summary>
public sealed class OutboxInspector
{
    // Work item: TASK-083 (FEAT-019)
    /// <summary>
    /// The most rows one read returns. A scenario window holds one to three events; the cap only bounds a careless
    /// call.
    /// </summary>
    public const int MaxRows = 500;

    private readonly DefaultContext _context;

    // Work item: TASK-083 (FEAT-019)
    /// <summary>
    /// Initializes a new instance of OutboxInspector
    /// </summary>
    /// <param name="context">The database context</param>
    public OutboxInspector(DefaultContext context)
    {
        _context = context;
    }

    // Work item: TASK-083 (FEAT-019)
    /// <summary>
    /// Returns the highest outbox sequence, or 0 when the table is empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The head sequence</returns>
    public async Task<long> HeadAsync(CancellationToken cancellationToken) =>
        await _context.OutboxMessages.AsNoTracking().MaxAsync(message => (long?)message.Sequence, cancellationToken) ?? 0;

    // Work item: TASK-083 (FEAT-019)
    /// <summary>
    /// Returns up to <see cref="MaxRows"/> rows with a sequence greater than the given one, in sequence order.
    /// </summary>
    /// <param name="sequence">The last sequence the caller has seen; 0 for none</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The later rows</returns>
    public async Task<IReadOnlyList<OutboxMessage>> ReadAfterAsync(long sequence, CancellationToken cancellationToken) =>
        await _context.OutboxMessages.AsNoTracking()
            .Where(message => message.Sequence > sequence)
            .OrderBy(message => message.Sequence)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);
}
#endif
