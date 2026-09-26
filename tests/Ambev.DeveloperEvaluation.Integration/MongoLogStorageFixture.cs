using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Logging;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using Serilog.Core;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-033 (FEAT-016)
/// <summary>
/// Points the production logging wiring at a throwaway MongoDB database and drops it after the tests.
/// The server comes from <c>ConnectionStrings:LogStorage</c>, read from the WebApi appsettings and
/// overridden by the <c>ConnectionStrings__LogStorage</c> environment variable.
/// </summary>
public sealed class MongoLogStorageFixture : IAsyncLifetime
{
    /// <summary>
    /// The environment name the test loggers stamp on every event.
    /// </summary>
    public const string EnvironmentName = "Integration";

    /// <summary>
    /// The application name the test loggers stamp on every event.
    /// </summary>
    public const string ApplicationName = "Ambev.DeveloperEvaluation.Integration";

    private IConfiguration _configuration = null!;

    /// <summary>
    /// Gets the settings of the throwaway database.
    /// </summary>
    public LogStorageSettings Settings { get; private set; } = null!;

    /// <summary>
    /// Loads the WebApi configuration and replaces the log database with a new throwaway name.
    /// </summary>
    public Task InitializeAsync()
    {
        var webApiDirectory = PostgresFixture.FindWebApiDirectory();
        var appConfiguration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        _configuration = new ConfigurationBuilder()
            .AddConfiguration(appConfiguration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [LogStorageSettings.DatabaseKey] = $"integration_logs_{Guid.NewGuid():N}"
            })
            .Build();

        Settings = LogStorageSettings.FromConfiguration(_configuration);
        return Task.CompletedTask;
    }

    // Work item: TASK-033 (FEAT-016)
    /// <summary>
    /// Builds a logger with the production wiring, writing to the given collection of the throwaway database.
    /// </summary>
    /// <param name="collection">The collection to write to</param>
    /// <param name="expireAfter">Replaces the configured TTL when set</param>
    /// <param name="connectionString">Replaces the configured MongoDB URL when set</param>
    public Logger CreateLogger(string collection, TimeSpan? expireAfter = null, string? connectionString = null)
    {
        var overrides = new Dictionary<string, string?> { [LogStorageSettings.CollectionKey] = collection };
        if (expireAfter is not null)
            overrides[LogStorageSettings.ExpireAfterKey] = expireAfter.Value.ToString("c", CultureInfo.InvariantCulture);
        if (connectionString is not null)
            overrides[LogStorageSettings.ConnectionStringKey] = connectionString;

        var configuration = new ConfigurationBuilder()
            .AddConfiguration(_configuration)
            .AddInMemoryCollection(overrides)
            .Build();

        return new LoggerConfiguration()
            .ConfigureDefaultLogging(configuration, EnvironmentName, ApplicationName)
            .CreateLogger();
    }

    /// <summary>
    /// Opens a collection of the throwaway database.
    /// </summary>
    public IMongoCollection<BsonDocument> Collection(string name) =>
        new MongoClient(Settings.ConnectionString).GetDatabase(Settings.Database).GetCollection<BsonDocument>(name);

    /// <summary>
    /// Drops the throwaway database.
    /// </summary>
    public Task DisposeAsync() =>
        new MongoClient(Settings.ConnectionString).DropDatabaseAsync(Settings.Database);
}
