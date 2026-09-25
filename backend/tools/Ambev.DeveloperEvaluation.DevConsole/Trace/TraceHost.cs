using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-045 (FEAT-017)
/// <summary>
/// Hosts the WebApi in this process. The whole Program.cs runs: migrations, the administrator seed, the bus, and the
/// outbox relay. The given settings enter as host configuration before Program runs, which is the only way they
/// reach AddDbContext and AddMessaging.
/// </summary>
public sealed class TraceHost : WebApplicationFactory<Ambev.DeveloperEvaluation.WebApi.Program>
{
    private readonly IReadOnlyDictionary<string, string?> _hostSettings;

    /// <summary>
    /// Initializes a new instance of TraceHost
    /// </summary>
    /// <param name="hostSettings">Configuration keys and values that override the WebApi appsettings</param>
    public TraceHost(IReadOnlyDictionary<string, string?> hostSettings)
    {
        _hostSettings = hostSettings;
    }

    /// <summary>
    /// Points the content root at the WebApi project, found from the build output through the solution file. Under
    /// dotnet run the factory's own lookup falls back to the solution directory plus the WebApi assembly name, a
    /// directory that does not exist, and the host fails to start.
    /// </summary>
    /// <param name="builder">The web host builder</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSolutionRelativeContentRoot("src/Ambev.DeveloperEvaluation.WebApi");
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(_hostSettings));
        return base.CreateHost(builder);
    }
}
