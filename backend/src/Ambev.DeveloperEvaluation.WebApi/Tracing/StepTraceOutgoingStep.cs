#if DEBUG
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-055 (FEAT-017)
/// <summary>
/// Traces SAL-BUS-01 for every message sent to the queue, from the controller and from the outbox relay. The line prints
/// before the transport insert, which happens when the send transaction commits right after the pipeline. It always calls
/// <c>next</c>. Debug builds only.
/// </summary>
public sealed class StepTraceOutgoingStep : IOutgoingStep
{
    // Work item: TASK-055 (FEAT-017), TASK-059 (FEAT-017)
    /// <inheritdoc />
    public Task Process(OutgoingStepContext context, Func<Task> next)
    {
        if (StepTrace.Sink is null)
            return next();

        var message = context.Load<Message>();
        var body = message?.Body;
        var saleId = body is CreateSaleCommand command ? command.Id : SaleEventIds.SaleIdOf(body);
        var destinations = context.Load<DestinationAddresses>();
        StepTrace.Step("SAL-BUS-01", "Queued in sales-intake",
            [("messageId", message?.Headers.GetValueOrDefault(Headers.MessageId)), ("eventType", body?.GetType().Name), ("saleId", saleId),
             ("queue", destinations is null ? null : string.Join(",", destinations))]);
        return next();
    }
}
#endif
