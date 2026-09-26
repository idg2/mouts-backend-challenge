using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.ReadModel;

// Work item: TASK-074 (FEAT-003)
/// <summary>
/// Where the sale read model lives in MongoDB. Every value is required; there is no default in code.
/// </summary>
public sealed class ReadModelSettings
{
    /// <summary>
    /// Configuration key of the MongoDB URL.
    /// </summary>
    public const string ConnectionStringKey = "ConnectionStrings:ReadModel";

    /// <summary>
    /// Configuration key of the database that holds the sales collection.
    /// </summary>
    public const string DatabaseKey = "ReadModel:Database";

    /// <summary>
    /// Configuration key of the sales collection.
    /// </summary>
    public const string CollectionKey = "ReadModel:Collection";

    private ReadModelSettings(string connectionString, string database, string collection)
    {
        ConnectionString = connectionString;
        Database = database;
        Collection = collection;
    }

    /// <summary>
    /// Gets the MongoDB URL.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets the database that holds the sales collection.
    /// </summary>
    public string Database { get; }

    /// <summary>
    /// Gets the sales collection.
    /// </summary>
    public string Collection { get; }

    /// <summary>
    /// Reads the settings, throwing <see cref="InvalidOperationException"/> naming the first missing or invalid key.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The validated settings</returns>
    public static ReadModelSettings FromConfiguration(IConfiguration configuration)
    {
        var connectionString = Required(configuration, ConnectionStringKey);
        try
        {
            MongoUrl.Create(connectionString);
        }
        catch (MongoConfigurationException)
        {
            // The driver's message is left out: for some malformed URLs it contains the password.
            throw new InvalidOperationException(
                $"{ConnectionStringKey} is not a valid MongoDB URL. Use mongodb://user:password@host:port/?options and " +
                "percent-encode reserved characters (@ : / ? #) in the user and password, for example '@' as %40.");
        }

        return new ReadModelSettings(connectionString, Required(configuration, DatabaseKey), Required(configuration, CollectionKey));
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{key} is not configured. Set it in appsettings or via the {key.Replace(":", "__")} environment variable.");

        return value;
    }
}
