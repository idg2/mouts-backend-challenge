using System.Globalization;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.Common.Logging;

// Work item: TASK-033 (FEAT-016)
/// <summary>
/// Where the application stores its logs in MongoDB and how long they live. Every value is required.
/// </summary>
public sealed class LogStorageSettings
{
    /// <summary>
    /// Configuration key of the MongoDB URL.
    /// </summary>
    public const string ConnectionStringKey = "ConnectionStrings:LogStorage";

    /// <summary>
    /// Configuration key of the database that holds the log collection.
    /// </summary>
    public const string DatabaseKey = "LogStorage:Database";

    /// <summary>
    /// Configuration key of the log collection.
    /// </summary>
    public const string CollectionKey = "LogStorage:Collection";

    /// <summary>
    /// Configuration key of the time span after which a stored log expires.
    /// </summary>
    public const string ExpireAfterKey = "LogStorage:ExpireAfter";

    private LogStorageSettings(string connectionString, string database, string collection, TimeSpan expireAfter)
    {
        ConnectionString = connectionString;
        Database = database;
        Collection = collection;
        ExpireAfter = expireAfter;
    }

    /// <summary>
    /// Gets the MongoDB URL.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets the database that holds the log collection.
    /// </summary>
    public string Database { get; }

    /// <summary>
    /// Gets the log collection.
    /// </summary>
    public string Collection { get; }

    /// <summary>
    /// Gets the time span after which a stored log expires.
    /// </summary>
    public TimeSpan ExpireAfter { get; }

    /// <summary>
    /// Reads the settings, throwing <see cref="InvalidOperationException"/> naming the first missing or invalid key.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The validated settings</returns>
    public static LogStorageSettings FromConfiguration(IConfiguration configuration)
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

        var database = Required(configuration, DatabaseKey);
        var collection = Required(configuration, CollectionKey);

        var expireAfterText = Required(configuration, ExpireAfterKey);
        if (!TimeSpan.TryParse(expireAfterText, CultureInfo.InvariantCulture, out var expireAfter) || expireAfter <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"{ExpireAfterKey} must be a positive time span such as \"1.00:00:00\"; got \"{expireAfterText}\".");

        return new LogStorageSettings(connectionString, database, collection, expireAfter);
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
