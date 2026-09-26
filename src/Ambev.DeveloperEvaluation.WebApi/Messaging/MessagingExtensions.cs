using Ambev.DeveloperEvaluation.Domain.Events;
using FluentValidation;
using Rebus.Config;
using Rebus.Retry.FailFast;
#if DEBUG
using Ambev.DeveloperEvaluation.WebApi.Tracing;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry;
using Rebus.Retry.Simple;
#endif

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-038 (FEAT-006), TASK-031 (FEAT-004)
/// <summary>
/// Registers Rebus over the MongoDB transport, the message handlers of this assembly, and the outbox relay that feeds the bus.
/// </summary>
public static class MessagingExtensions
{
    // Work item: TASK-031 (FEAT-004), TASK-055 (FEAT-017)
    /// <summary>
    /// Adds the bus. The API sends to its own input queue and consumes it with at most
    /// <see cref="MessagingSettings.MaxParallelism"/> messages at a time. A <see cref="ValidationException"/> is
    /// permanent, so it moves the message to the error queue without retries. It also registers the outbox relay service,
    /// which sends pending outbox rows to the same queue through <see cref="RebusEventPublisher"/>.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The same builder</returns>
    public static WebApplicationBuilder AddMessaging(this WebApplicationBuilder builder)
    {
        var settings = MessagingSettings.FromConfiguration(builder.Configuration);

        builder.Services.AutoRegisterHandlersFromAssemblyOf<CreateSaleMessageHandler>();
        builder.Services.AddRebus(configure => configure
            .Transport(t => t.UseMongoDb(new MongoDbTransportOptions(settings.ConnectionString), settings.InputQueue))
            .Options(o =>
            {
                o.SetNumberOfWorkers(settings.Workers);
                o.SetMaxParallelism(settings.MaxParallelism);
                o.FailFastOn<ValidationException>();
#if DEBUG
                // Trace-only hooks (StepTrace): a receive step before dispatch, a send step before the transport, and
                // a decorator on the error handler. None of this exists in Release.
                o.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                    .OnReceive(
                        new StepTraceIncomingStep(c.Get<IErrorTracker>(), c.Get<IFailFastChecker>(), c.Get<RetryStrategySettings>()),
                        PipelineRelativePosition.Before,
                        typeof(DispatchIncomingMessageStep))
                    .OnSend(new StepTraceOutgoingStep(), PipelineRelativePosition.Before, typeof(SendOutgoingMessageStep)));
                o.Decorate<IErrorHandler>(c => new StepTraceErrorHandler(c.Get<IErrorHandler>()));
#endif
            }));

        // After AddRebus: hosted services start in registration order, so the bus is up before the relay's first cycle.
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<IEventPublisher, RebusEventPublisher>();
        builder.Services.AddHostedService<OutboxRelayService>();

        return builder;
    }
}
