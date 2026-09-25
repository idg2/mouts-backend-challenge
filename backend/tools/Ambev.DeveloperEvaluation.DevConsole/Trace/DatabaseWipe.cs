using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Npgsql;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// Empties the development databases before a trace run: drops the PostgreSQL database (the API recreates it through
/// its migrations and reseeds the administrator) and the MongoDB queue database (pending messages and the error queue).
/// The log database is untouched. Never forces the drop: another process holding the database is a setup error.
/// </summary>
public static class DatabaseWipe
{
    /// <summary>
    /// The message printed when PostgreSQL refuses the drop because other sessions hold the database.
    /// </summary>
    public const string HeldMessage =
        "The PostgreSQL database is in use by other sessions, so it was not dropped. Stop the API first: from backend, " +
        "run \"docker compose stop ambev.developerevaluation.webapi\" and stop any \"dotnet run\" of the WebApi, then run the console again.";

    private const string PostgresInUse = "55006";

    /// <summary>
    /// Reads the database names from the WebApi connection strings.
    /// </summary>
    /// <param name="configuration">The merged configuration</param>
    /// <returns>The PostgreSQL and the MongoDB queue database names</returns>
    public static (string Postgres, string Mongo) Names(IConfiguration configuration)
    {
        var postgres = new NpgsqlConnectionStringBuilder(Required(configuration, "ConnectionStrings:DefaultConnection")).Database
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection has no Database.");
        var mongo = MongoUrl.Create(Required(configuration, "ConnectionStrings:MessageBus")).DatabaseName
            ?? throw new InvalidOperationException("ConnectionStrings:MessageBus has no database in its path.");
        return (postgres, mongo);
    }

    // Work item: TASK-056 (FEAT-017), TASK-059 (FEAT-017)
    /// <summary>
    /// Names the databases, asks for confirmation, and drops both. The PostgreSQL drop runs from the maintenance
    /// database, because a session cannot drop the database it is connected to. EF Core's EnsureDeletedAsync is not
    /// used: the Npgsql provider terminates the other sessions first, which would force the drop.
    /// </summary>
    /// <param name="configuration">The merged configuration</param>
    /// <param name="maintenanceDatabase">The PostgreSQL database to connect to while dropping (Trace:MaintenanceDatabase)</param>
    /// <param name="output">Where messages go</param>
    /// <param name="error">Where failures go</param>
    /// <param name="confirm">Asks the user; returns true to go on</param>
    /// <returns>0 when both were dropped, 1 otherwise</returns>
    public static Task<int> RunAsync(IConfiguration configuration, string maintenanceDatabase, TextWriter output, TextWriter error, Func<bool> confirm)
    {
        var names = Names(configuration);
        var postgresConnection = Required(configuration, "ConnectionStrings:DefaultConnection");
        var mongoConnection = Required(configuration, "ConnectionStrings:MessageBus");
        return RunAsync(
            names,
            output,
            error,
            confirm,
            async () =>
            {
                await using var connection = new NpgsqlConnection(MaintenanceConnectionString(postgresConnection, maintenanceDatabase));
                await connection.OpenAsync();
                await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{names.Postgres.Replace("\"", "\"\"")}\"", connection);
                await drop.ExecuteNonQueryAsync();
            },
            async () =>
            {
                using var client = new MongoClient(mongoConnection);
                await client.DropDatabaseAsync(names.Mongo);
            });
    }

    // Work item: TASK-056 (FEAT-017)
    /// <summary>
    /// Points the application connection string at the maintenance database, without pooling, keeping the server and
    /// the credentials.
    /// </summary>
    /// <param name="connectionString">The application connection string</param>
    /// <param name="maintenanceDatabase">The database to connect to while dropping the application one</param>
    /// <returns>The maintenance connection string</returns>
    public static string MaintenanceConnectionString(string connectionString, string maintenanceDatabase) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = maintenanceDatabase, Pooling = false }.ConnectionString;

    // Work item: TASK-056 (FEAT-017), TASK-059 (FEAT-017)
    /// <summary>
    /// The testable core: prints the names, confirms, and runs the two drops; a failure goes to the error writer.
    /// </summary>
    /// <param name="names">The database names</param>
    /// <param name="output">Where messages go</param>
    /// <param name="error">Where failures go</param>
    /// <param name="confirm">Asks the user</param>
    /// <param name="dropPostgres">Drops the PostgreSQL database</param>
    /// <param name="dropMongo">Drops the MongoDB queue database</param>
    /// <returns>0 when both were dropped, 1 otherwise</returns>
    public static async Task<int> RunAsync(
        (string Postgres, string Mongo) names, TextWriter output, TextWriter error, Func<bool> confirm, Func<Task> dropPostgres, Func<Task> dropMongo)
    {
        output.WriteLine($"This run wipes the PostgreSQL database \"{names.Postgres}\" and the MongoDB queue database \"{names.Mongo}\".");
        if (!confirm())
        {
            output.WriteLine("Cancelled.");
            return 1;
        }

        try
        {
            await dropPostgres();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresInUse)
        {
            error.WriteLine(HeldMessage);
            return 1;
        }

        try
        {
            await dropMongo();
        }
        catch (Exception exception)
        {
            error.WriteLine(
                $"The PostgreSQL database \"{names.Postgres}\" was dropped, but the MongoDB queue database \"{names.Mongo}\" was not: " +
                $"{exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}");
            return 1;
        }

        output.WriteLine("Both databases dropped.");
        return 0;
    }

    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"{key} is not configured.");
}
