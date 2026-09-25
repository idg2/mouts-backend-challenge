using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-031 (FEAT-004)
/// <summary>
/// Consumes the sale events and writes them to the application log. Logging twice is harmless, so a re-delivered
/// event needs no deduplication here.
/// </summary>
public class SaleEventLogHandler :
    IHandleMessages<SaleCreated>,
    IHandleMessages<SaleModified>,
    IHandleMessages<SaleCancelled>,
    IHandleMessages<ItemCancelled>,
    IHandleMessages<SaleDeleted>
{
    private readonly ILogger<SaleEventLogHandler> _logger;

    /// <summary>
    /// Initializes a new instance of SaleEventLogHandler
    /// </summary>
    /// <param name="logger">The logger</param>
    public SaleEventLogHandler(ILogger<SaleEventLogHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Handle(SaleCreated message) => Log(nameof(SaleCreated), message.Sale.SaleId);

    /// <inheritdoc />
    public Task Handle(SaleModified message) => Log(nameof(SaleModified), message.Sale.SaleId);

    /// <inheritdoc />
    public Task Handle(SaleCancelled message) => Log(nameof(SaleCancelled), message.SaleId);

    /// <inheritdoc />
    public Task Handle(ItemCancelled message) => Log(nameof(ItemCancelled), message.SaleId);

    /// <inheritdoc />
    public Task Handle(SaleDeleted message) => Log(nameof(SaleDeleted), message.SaleId);

    // Work item: TASK-054 (FEAT-017)
    private Task Log(string eventType, Guid saleId)
    {
        var messageId = MessageContext.Current.Headers[Headers.MessageId];
        StepTrace.Step("SAL-CON-02", "Handle the event and read its message id",
            [("eventType", eventType), ("messageId", messageId), ("saleId", saleId)]);
        _logger.LogInformation("Sale event {EventType} {MessageId} for sale {SaleId}",
            eventType, messageId, saleId);
        StepTrace.Step("SAL-CON-03", "Log the event type, message id, and sale id",
            [("eventType", eventType), ("messageId", messageId), ("saleId", saleId)]);
        return Task.CompletedTask;
    }
}
