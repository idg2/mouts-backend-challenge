#if DEBUG
using Ambev.DeveloperEvaluation.Common.Tracing;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Transport;

namespace Ambev.DeveloperEvaluation.WebApi.Tracing;

// Work item: TASK-055 (FEAT-017)
/// <summary>
/// Traces SAL-BUS-07 before Rebus's own error handler moves the message to the error queue, then always delegates to it.
/// Debug builds only.
/// </summary>
public sealed class StepTraceErrorHandler : IErrorHandler
{
    private readonly IErrorHandler _inner;

    /// <summary>
    /// Initializes a new instance of StepTraceErrorHandler
    /// </summary>
    /// <param name="inner">The decorated handler (the dead-letter queue handler)</param>
    public StepTraceErrorHandler(IErrorHandler inner)
    {
        _inner = inner;
    }

    // Work item: TASK-055 (FEAT-017), TASK-059 (FEAT-017)
    /// <inheritdoc />
    public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
    {
        if (StepTrace.Sink is null)
            return _inner.HandlePoisonMessage(transportMessage, transactionContext, exception);

        // Rebus also dead-letters a message that has no id, so the headers are read without throwing. Rebus's retry
        // step always passes an AggregateException wrapper whose inner types exist only as free text in Details, so
        // error prints the wrapper; SAL-BUS-05 and SAL-BUS-06 already print the handler's exception type.
        var headers = transportMessage.Headers;
        StepTrace.Step("SAL-BUS-07", "Moved to the error queue",
            [("messageId", headers.GetValueOrDefault(Headers.MessageId)), ("eventType", SimpleName(headers.GetValueOrDefault(Headers.Type))),
             ("error", SimpleName(exception.Type)), ("reason", exception.Message)]);
        return _inner.HandlePoisonMessage(transportMessage, transactionContext, exception);
    }

    // Work item: TASK-059 (FEAT-017)
    // "Namespace.Type, Assembly" -> "Type", like every other line prints a type.
    private static string? SimpleName(string? typeName)
    {
        if (typeName is null)
            return null;

        var comma = typeName.IndexOf(',');
        var fullName = comma >= 0 ? typeName[..comma] : typeName;
        return fullName[(fullName.LastIndexOf('.') + 1)..].Trim();
    }
}
#endif
