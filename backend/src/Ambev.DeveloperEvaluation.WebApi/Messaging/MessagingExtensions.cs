using FluentValidation;
using Rebus.Config;
using Rebus.Retry.FailFast;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-038 (FEAT-006)
/// <summary>
/// Registers Rebus over the MongoDB transport and the message handlers of this assembly.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Adds the bus. The API sends to its own input queue and consumes it with at most
    /// <see cref="MessagingSettings.MaxParallelism"/> messages at a time. A <see cref="ValidationException"/> is
    /// permanent, so it moves the message to the error queue without retries.
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
            }));

        return builder;
    }
}
