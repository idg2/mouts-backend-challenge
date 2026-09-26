#if DEBUG
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Retry;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;
using Rebus.Transport;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-055 (FEAT-017)
/// <summary>
/// Traces the receive side of the bus (SAL-BUS-02 to 06, SAL-ASY-04, 07, 08, SAL-CON-01, 04). Injected before the
/// dispatch step, so it sees the typed message, the handlers, and every handler exception on its way to the retry step.
/// It only observes: <c>next</c> runs once and every exception is rethrown. Debug builds only.
/// </summary>
public sealed class StepTraceIncomingStep : IIncomingStep
{
    private readonly IErrorTracker _errorTracker;
    private readonly IFailFastChecker _failFastChecker;
    private readonly RetryStrategySettings _retrySettings;

    /// <summary>
    /// Initializes a new instance of StepTraceIncomingStep
    /// </summary>
    /// <param name="errorTracker">Rebus's per-message failure memory, used for the delivery attempt</param>
    /// <param name="failFastChecker">Decides whether an exception skips the retries</param>
    /// <param name="retrySettings">The maximum number of deliveries</param>
    public StepTraceIncomingStep(IErrorTracker errorTracker, IFailFastChecker failFastChecker, RetryStrategySettings retrySettings)
    {
        _errorTracker = errorTracker;
        _failFastChecker = failFastChecker;
        _retrySettings = retrySettings;
    }

    // Work item: TASK-055 (FEAT-017), TASK-059 (FEAT-017)
    /// <inheritdoc />
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        if (StepTrace.Sink is null)
        {
            await next();
            return;
        }

        var message = context.Load<Message>();
        var transaction = context.Load<ITransactionContext>();
        var messageId = message?.Headers.GetValueOrDefault(Headers.MessageId);
        var body = message?.Body;
        var type = body?.GetType().Name;
        var saleId = body is CreateSaleCommand command ? command.Id : SaleEventIds.SaleIdOf(body);
        // DefaultRetryStep dead-letters a message without an id before this step, so the id is present here.
        var attempt = messageId is null ? 1 : (await _errorTracker.GetExceptions(messageId)).Count + 1;

        StepTrace.Step("SAL-BUS-02", "Leased by a worker", [("messageId", messageId), ("eventType", type), ("attempt", attempt)]);
        if (body is CreateSaleCommand)
            StepTrace.Step("SAL-ASY-04", "A worker receives the command in a new DI scope",
                [("messageId", messageId), ("saleId", saleId), ("attempt", attempt)]);
        else if (body is IIntegrationEvent)
            StepTrace.Step("SAL-CON-01", "Receive an event message", [("messageId", messageId), ("eventType", type), ("saleId", saleId)]);

        var handled = false;
        transaction?.OnAck(_ =>
        {
            // The dead-letter path also acks; only a handled message was deleted as "handled".
            if (!handled)
                return Task.CompletedTask;

            StepTrace.Step("SAL-BUS-04", "Handled and deleted", [("messageId", messageId), ("eventType", type)]);
            if (body is CreateSaleCommand)
                StepTrace.Step("SAL-ASY-07", "Message removed from the queue", [("messageId", messageId), ("saleId", saleId)]);
            else if (body is IIntegrationEvent)
                StepTrace.Step("SAL-CON-04", "Delete the message", [("messageId", messageId), ("eventType", type), ("saleId", saleId)]);
            return Task.CompletedTask;
        });

        StepTrace.Step("SAL-BUS-03", "Dispatched to the handler of its type",
            [("messageId", messageId), ("eventType", type), ("handlers", HandlerNames(context.Load<HandlerInvokers>()))]);
        try
        {
            await next();
            handled = true;
        }
        catch (Exception exception)
        {
            // The retry step asks the same checker right after this rethrow, so the answer matches its decision.
            var failFast = _failFastChecker.ShouldFailFast(messageId ?? string.Empty, exception);
            if (body is CreateSaleCommand)
                StepTrace.Step("SAL-ASY-08", "Failure goes to the retry policy",
                    [("messageId", messageId), ("saleId", saleId), ("attempt", attempt), ("error", exception), ("failFast", failFast)]);
            if (failFast)
                StepTrace.Step("SAL-BUS-05", "Fail fast", [("messageId", messageId), ("error", exception)]);
            else
                StepTrace.Step("SAL-BUS-06", "Retried up to 5 deliveries",
                    [("messageId", messageId), ("attempt", attempt), ("maxAttempts", _retrySettings.MaxDeliveryAttempts), ("final", attempt >= _retrySettings.MaxDeliveryAttempts)]);
            throw;
        }
    }

    private static string HandlerNames(HandlerInvokers? invokers)
    {
        if (invokers is null)
            return string.Empty;

        var names = new List<string>();
        for (var index = 0; index < invokers.Count; index++)
            names.Add(invokers[index].Handler?.GetType().Name ?? "null");

        return string.Join(",", names);
    }
}
#endif
