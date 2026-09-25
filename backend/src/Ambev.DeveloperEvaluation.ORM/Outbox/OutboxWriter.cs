using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
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

    // Work item: TASK-054 (FEAT-017)
    /// <inheritdoc />
    public async Task EnqueueAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        StepTrace.Step("SAL-OBW-01", "EnqueueAsync with the event",
            [("eventType", integrationEvent.GetType().Name), ("saleId", SaleEventIds.SaleIdOf(integrationEvent)), ("inTransaction", _context.Database.CurrentTransaction is not null)]);
        if (_context.Database.CurrentTransaction is null)
        {
            StepTrace.Step("SAL-OBW-02", "InvalidOperationException", [("eventType", integrationEvent.GetType().Name), ("inTransaction", false)]);
            throw new InvalidOperationException(
                "Outbox writes must run inside the command transaction; mark the command as ITransactionalCommand.");
        }

        var typeName = IntegrationEventTypes.NameOf(integrationEvent);
        StepTrace.Step("SAL-OBW-03", "Resolve the type name", [("eventType", typeName)]);
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        StepTrace.Step("SAL-OBW-04", "Serialize the payload as JSON", [("eventType", typeName), ("payloadLength", payload.Length)]);
        var row = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeName,
            Payload = payload,
            OccurredAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        _context.OutboxMessages.Add(row);
        StepTrace.Step("SAL-OBW-05", "Add the row with a new id and OccurredAt",
            [("rowId", row.Id), ("eventType", row.Type), ("saleId", SaleEventIds.SaleIdOf(integrationEvent)), ("occurredAt", row.OccurredAt)]);
        await _context.SaveChangesAsync(cancellationToken);
        StepTrace.Step("SAL-OBW-06", "SaveChangesAsync",
            [("rowId", row.Id), ("sequence", row.Sequence), ("saleId", SaleEventIds.SaleIdOf(integrationEvent)), ("eventType", row.Type)]);
    }
}
