namespace Ambev.DeveloperEvaluation.ORM.Outbox;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// One integration event waiting in, or already dispatched from, the transactional outbox.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Gets or sets the event id, sent as the message id so consumers can deduplicate.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the dispatch order, assigned by the database at insert.
    /// </summary>
    public long Sequence { get; set; }

    /// <summary>
    /// Gets or sets the event name registered in <c>IntegrationEventTypes</c>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event serialized as JSON.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time the event was enqueued.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC time the event was dispatched; null while pending.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
