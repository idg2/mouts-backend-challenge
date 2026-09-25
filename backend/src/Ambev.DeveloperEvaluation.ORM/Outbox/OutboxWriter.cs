using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.ORM.Outbox;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// Implementation of IOutbox over the scoped <see cref="DefaultContext"/>: the row is saved in the transaction the
/// command's <c>TransactionBehavior</c> opened, so it commits or rolls back with the business write.
/// </summary>
public class OutboxWriter : IOutbox
{
    private readonly DefaultContext _context;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of OutboxWriter
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="timeProvider">The source of the enqueue time</param>
    public OutboxWriter(DefaultContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "Outbox writes must run inside the command transaction; mark the command as ITransactionalCommand.");

        _context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = IntegrationEventTypes.NameOf(integrationEvent),
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            OccurredAt = _timeProvider.GetUtcNow().UtcDateTime
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
