using Ambev.DeveloperEvaluation.ORM.ReadModel;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-075 (FEAT-003)
/// <summary>
/// Points the sale read store at a throwaway MongoDB database and drops it after the tests. The server comes from
/// <c>ConnectionStrings:ReadModel</c>, read from the WebApi appsettings and overridden by the
/// <c>ConnectionStrings__ReadModel</c> environment variable.
/// </summary>
public sealed class MongoReadModelFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the settings of the throwaway database.
    /// </summary>
    public ReadModelSettings Settings { get; private set; } = null!;

    /// <summary>
    /// Gets the client the store and the tests share.
    /// </summary>
    public IMongoClient Client { get; private set; } = null!;

    /// <summary>
    /// Loads the WebApi configuration and replaces the read model database with a new throwaway name.
    /// </summary>
    public Task InitializeAsync()
    {
        var webApiDirectory = PostgresFixture.FindWebApiDirectory();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ReadModelSettings.DatabaseKey] = $"integration_read_{Guid.NewGuid():N}"
            })
            .Build();

        Settings = ReadModelSettings.FromConfiguration(configuration);
        Client = new MongoClient(Settings.ConnectionString);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a store over the throwaway database.
    /// </summary>
    public SaleReadStore CreateStore() => new(Client, Settings);

    /// <summary>
    /// Opens the raw sales collection of the throwaway database.
    /// </summary>
    public IMongoCollection<BsonDocument> RawCollection() =>
        Client.GetDatabase(Settings.Database).GetCollection<BsonDocument>(Settings.Collection);

    /// <summary>
    /// Drops the throwaway database.
    /// </summary>
    public Task DisposeAsync() => Client.DropDatabaseAsync(Settings.Database);
}
