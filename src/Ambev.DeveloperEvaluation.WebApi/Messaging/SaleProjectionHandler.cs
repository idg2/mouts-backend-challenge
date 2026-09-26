using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-076 (FEAT-003)
/// <summary>
/// Keeps the MongoDB read model from the sale events: SaleCreated and SaleModified store the snapshot they carry,
/// SaleDeleted leaves a tombstone, and the two cancellation events change nothing because the SaleModified written
/// with them already carries the resulting state. Each write is ordered by the outbox sequence header, so events
/// of one sale handled in parallel or delivered again cannot leave an older state.
/// </summary>
public class SaleProjectionHandler :
    IHandleMessages<SaleCreated>,
    IHandleMessages<SaleModified>,
    IHandleMessages<SaleCancelled>,
    IHandleMessages<ItemCancelled>,
    IHandleMessages<SaleDeleted>
{
    private readonly ISaleReadStore _store;
    private readonly IMessageContext _messageContext;

    /// <summary>
    /// Initializes a new instance of SaleProjectionHandler
    /// </summary>
    /// <param name="store">The read model</param>
    /// <param name="messageContext">The context of the message being handled, for its headers</param>
    public SaleProjectionHandler(ISaleReadStore store, IMessageContext messageContext)
    {
        _store = store;
        _messageContext = messageContext;
    }

    /// <inheritdoc />
    public Task Handle(SaleCreated message) =>
        ApplyAsync(nameof(SaleCreated), message.Sale.SaleId, "upsert", sequence => _store.UpsertAsync(message.Sale, sequence));

    /// <inheritdoc />
    public Task Handle(SaleModified message) =>
        ApplyAsync(nameof(SaleModified), message.Sale.SaleId, "upsert", sequence => _store.UpsertAsync(message.Sale, sequence));

    /// <inheritdoc />
    public Task Handle(SaleDeleted message) =>
        ApplyAsync(nameof(SaleDeleted), message.SaleId, "tombstone", sequence => _store.MarkDeletedAsync(message.SaleId, sequence));

    /// <inheritdoc />
    public Task Handle(SaleCancelled message) =>
        ApplyAsync(nameof(SaleCancelled), message.SaleId, "none", _ => Task.FromResult(true));

    /// <inheritdoc />
    public Task Handle(ItemCancelled message) =>
        ApplyAsync(nameof(ItemCancelled), message.SaleId, "none", _ => Task.FromResult(true));

    private async Task ApplyAsync(string eventType, Guid saleId, string action, Func<long, Task<bool>> apply)
    {
        var sequence = Sequence();
        var applied = await apply(sequence);
        StepTrace.Step("SAL-PRJ-01", "Apply the event to the read model",
            [("eventType", eventType), ("saleId", saleId), ("sequence", sequence), ("action", action), ("applied", applied)]);
        if (!applied)
            StepTrace.Step("SAL-PRJ-02", "Event older than the document, ignored", [("saleId", saleId), ("sequence", sequence)]);
    }

    // The header is set by RebusEventPublisher; a message without it did not come through the outbox relay of this
    // version and must not be applied with a made-up version.
    private long Sequence()
    {
        if (_messageContext.Headers.TryGetValue(RebusEventPublisher.SequenceHeader, out var raw)
            && long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            return sequence;

        throw new InvalidOperationException(
            $"The message has no valid '{RebusEventPublisher.SequenceHeader}' header; it was not sent by the outbox relay of this version.");
    }
}
