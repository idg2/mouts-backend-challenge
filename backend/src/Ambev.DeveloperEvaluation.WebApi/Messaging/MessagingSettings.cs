using System.Globalization;
using Ambev.DeveloperEvaluation.Common.Logging;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-038 (FEAT-006), TASK-031 (FEAT-004)
/// <summary>
/// Where the Rebus queue lives, how many messages the API consumes at a time, and how often the outbox relay feeds it.
/// Every value is required.
/// </summary>
public sealed class MessagingSettings
{
    /// <summary>
    /// Configuration key of the MongoDB URL; its path names the queue database.
    /// </summary>
    public const string ConnectionStringKey = "ConnectionStrings:MessageBus";

    /// <summary>
    /// Configuration key of the queue the API sends to and consumes from.
    /// </summary>
    public const string InputQueueKey = "Rebus:InputQueue";

    /// <summary>
    /// Configuration key of the number of Rebus worker threads.
    /// </summary>
    public const string WorkersKey = "Rebus:Workers";

    /// <summary>
    /// Configuration key of the maximum number of messages handled at the same time; keep it below the Npgsql pool size.
    /// </summary>
    public const string MaxParallelismKey = "Rebus:MaxParallelism";

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// Configuration key of the wait between outbox relay cycles, from <see cref="MinPollingInterval"/> to
    /// <see cref="MaxPollingInterval"/>.
    /// </summary>
    public const string PollingIntervalKey = "Outbox:PollingInterval";

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// The shortest accepted polling interval; below it the relay would query the database in a hot loop.
    /// </summary>
    public static readonly TimeSpan MinPollingInterval = TimeSpan.FromMilliseconds(100);

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// The longest accepted polling interval. It also rejects a bare number, which TimeSpan reads as days, and keeps
    /// the wait far below the limit of Task.Delay.
    /// </summary>
    public static readonly TimeSpan MaxPollingInterval = TimeSpan.FromHours(1);

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// Configuration key of the maximum number of outbox rows one relay cycle dispatches.
    /// </summary>
    public const string BatchSizeKey = "Outbox:BatchSize";

    // Characters MongoDB refuses in database names, plus the 63-character limit checked below.
    private static readonly char[] InvalidDatabaseNameCharacters = ['/', '\\', '.', ' ', '"', '$'];

    // Work item: TASK-031 (FEAT-004)
    private MessagingSettings(
        string connectionString, string inputQueue, int workers, int maxParallelism, TimeSpan pollingInterval, int batchSize)
    {
        ConnectionString = connectionString;
        InputQueue = inputQueue;
        Workers = workers;
        MaxParallelism = maxParallelism;
        PollingInterval = pollingInterval;
        BatchSize = batchSize;
    }

    /// <summary>
    /// Gets the MongoDB URL of the queue database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets the queue the API sends to and consumes from.
    /// </summary>
    public string InputQueue { get; }

    /// <summary>
    /// Gets the number of Rebus worker threads.
    /// </summary>
    public int Workers { get; }

    /// <summary>
    /// Gets the maximum number of messages handled at the same time.
    /// </summary>
    public int MaxParallelism { get; }

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// Gets the wait between outbox relay cycles.
    /// </summary>
    public TimeSpan PollingInterval { get; }

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// Gets the maximum number of outbox rows one relay cycle dispatches.
    /// </summary>
    public int BatchSize { get; }

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// Reads the settings, throwing <see cref="InvalidOperationException"/> naming the first missing or invalid key.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The validated settings</returns>
    public static MessagingSettings FromConfiguration(IConfiguration configuration)
    {
        var connectionString = Required(configuration, ConnectionStringKey);
        MongoUrl url;
        try
        {
            url = MongoUrl.Create(connectionString);
        }
        catch (MongoConfigurationException)
        {
            // The driver's message is left out: for some malformed URLs it contains the password.
            throw new InvalidOperationException(
                $"{ConnectionStringKey} is not a valid MongoDB URL. Use mongodb://user:password@host:port/database?options and " +
                "percent-encode reserved characters (@ : / ? #) in the user and password, for example '@' as %40.");
        }

        if (string.IsNullOrWhiteSpace(url.DatabaseName))
            throw new InvalidOperationException(
                $"{ConnectionStringKey} must name the queue database in its path, for example " +
                "mongodb://user:password@host:port/developer_evaluation_bus?authSource=admin.");

        if (url.DatabaseName.Length > 63 || url.DatabaseName.IndexOfAny(InvalidDatabaseNameCharacters) >= 0)
            throw new InvalidOperationException(
                $"{ConnectionStringKey} names the database \"{url.DatabaseName}\", which MongoDB refuses: use at most " +
                "63 characters and none of / \\ . space \" $.");

        if (string.Equals(url.DatabaseName, configuration[LogStorageSettings.DatabaseKey], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{ConnectionStringKey} names the database \"{url.DatabaseName}\", which {LogStorageSettings.DatabaseKey} " +
                "already uses for logs; give the queue its own database.");

        var inputQueue = Required(configuration, InputQueueKey);
        var workers = PositiveInteger(configuration, WorkersKey);
        var maxParallelism = PositiveInteger(configuration, MaxParallelismKey);
        var pollingInterval = BoundedTimeSpan(configuration, PollingIntervalKey, MinPollingInterval, MaxPollingInterval);
        var batchSize = PositiveInteger(configuration, BatchSizeKey);

        return new MessagingSettings(connectionString, inputQueue, workers, maxParallelism, pollingInterval, batchSize);
    }

    private static int PositiveInteger(IConfiguration configuration, string key)
    {
        var text = Required(configuration, key);
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)
            throw new InvalidOperationException($"{key} must be a positive integer; got \"{text}\".");

        return value;
    }

    // Work item: TASK-031 (FEAT-004)
    private static TimeSpan BoundedTimeSpan(IConfiguration configuration, string key, TimeSpan minimum, TimeSpan maximum)
    {
        var text = Required(configuration, key);
        if (!TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
            throw new InvalidOperationException(
                $"{key} must be a time span (hh:mm:ss) from {minimum:c} to {maximum:c}, such as \"00:00:05\"; got \"{text}\".");

        return value;
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
