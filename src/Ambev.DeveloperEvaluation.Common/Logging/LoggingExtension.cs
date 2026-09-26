using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Exceptions.Filters;
using Serilog.Templates;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.Common.Logging;



/// <summary> Add default Logging configuration to project. This configuration supports Serilog logs with DataDog compatible output.</summary>
public static class LoggingExtension
{
    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// The destructuring options builder configured with default destructurers and a custom DbUpdateExceptionDestructurer.
    /// Its <c>Entries</c> property is dropped because it carries every tracked entity value (e-mails, password hashes).
    /// </summary>
    static readonly DestructuringOptionsBuilder _destructuringOptionsBuilder = new DestructuringOptionsBuilder()
        .WithDefaultDestructurers()
        .WithDestructurers([new DbUpdateExceptionDestructurer()])
        .WithFilter(new IgnorePropertyByNameExceptionFilter("Entries"));

    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// Replaces the default logging provider with Serilog, configured by <see cref="ConfigureDefaultLogging"/>.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> to add services to.</param>
    /// <returns>A <see cref="WebApplicationBuilder"/> that can be used to further configure the API services.</returns>
    public static WebApplicationBuilder AddDefaultLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
            loggerConfiguration.ConfigureDefaultLogging(
                hostingContext.Configuration,
                builder.Environment.EnvironmentName,
                builder.Environment.ApplicationName));

        builder.Services.AddLogging();

        return builder;
    }

    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// Applies the <c>Serilog</c> configuration section (levels, console, enrichers, filters), the exception and
    /// host enrichers, and the MongoDB sink described by <see cref="LogStorageSettings"/>.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration to extend.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environmentName">The host environment name, stamped as <c>Environment</c>.</param>
    /// <param name="applicationName">The host application name, stamped as <c>Application</c>.</param>
    /// <returns>The same logger configuration.</returns>
    public static LoggerConfiguration ConfigureDefaultLogging(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string environmentName,
        string applicationName)
    {
        var storage = LogStorageSettings.FromConfiguration(configuration);

        // The MongoDB sink reports write failures only through SelfLog; without it, lost storage is silent.
        SelfLog.Enable(TextWriter.Synchronized(Console.Error));

        return loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Environment", environmentName)
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithExceptionDetails(_destructuringOptionsBuilder)
            .WriteTo.MongoDBBson(sink =>
            {
                sink.SetMongoDatabase(new MongoClient(storage.ConnectionString).GetDatabase(storage.Database));
                sink.SetCollectionName(storage.Collection);
                sink.SetExpireTTL(storage.ExpireAfter);
            });
    }

    // Work item: TASK-034 (FEAT-016)
    /// <summary>
    /// Writes one log event per HTTP request, with <c>RequestId</c> and <c>RemoteIpAddress</c>. Register it before
    /// the exception-handling middleware so the event carries the final status code.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance this method extends.</param>
    /// <returns>The same <see cref="WebApplication"/>.</returns>
    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
            });

        return app;
    }

    /// <summary>Adds middleware for Swagger documetation generation.</summary>
    /// <param name="app">The <see cref="WebApplication"/> instance this method extends.</param>
    /// <returns>The <see cref="WebApplication"/> for Swagger documentation.</returns>
    public static WebApplication UseDefaultLogging(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Logger>>();

        var mode = Debugger.IsAttached ? "Debug" : "Release";
        logger.LogInformation("Logging enabled for '{Application}' on '{Environment}' - Mode: {Mode}", app.Environment.ApplicationName, app.Environment.EnvironmentName, mode);
        return app;

    }
}
