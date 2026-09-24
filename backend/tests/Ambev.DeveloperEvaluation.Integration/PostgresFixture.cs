using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: BUG-008 (FEAT-010), TD-010 (FEAT-010)
/// <summary>
/// Creates a throwaway PostgreSQL database with every migration applied and drops it after the tests.
/// The server comes from <c>ConnectionStrings:DefaultConnection</c>, read from the WebApi appsettings and
/// overridden by the <c>ConnectionStrings__DefaultConnection</c> environment variable.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the connection string of the throwaway database.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Builds the connection string for a new database and applies the migrations to it.
    /// </summary>
    public async Task InitializeAsync()
    {
        var webApiDirectory = FindWebApiDirectory();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(baseConnection))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set it in the WebApi appsettings " +
                "or via the ConnectionStrings__DefaultConnection environment variable.");

        ConnectionString = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = $"integration_{Guid.NewGuid():N}"
        }.ConnectionString;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Creates a new context over the throwaway database.
    /// </summary>
    public DefaultContext CreateContext() =>
        new(new DbContextOptionsBuilder<DefaultContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>
    /// Drops the throwaway database.
    /// </summary>
    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    // Work item: TASK-033 (FEAT-016)
    internal static string FindWebApiDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ambev.DeveloperEvaluation.sln")))
            directory = directory.Parent;

        return directory is null
            ? throw new InvalidOperationException("Could not locate Ambev.DeveloperEvaluation.sln from the test output directory")
            : Path.Combine(directory.FullName, "src", "Ambev.DeveloperEvaluation.WebApi");
    }
}
