using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.LoadSimulator;

// Work item: TASK-040 (FEAT-006)
/// <summary>
/// One group of identical loops: how many run at the same time and how long each waits between requests.
/// </summary>
/// <param name="Name">The name shown in the report</param>
/// <param name="Loops">The number of concurrent loops</param>
/// <param name="PauseMilliseconds">The wait after each request of a loop</param>
public sealed record LoadProfile(string Name, int Loops, int PauseMilliseconds);

// Work item: TASK-040 (FEAT-006), BUG-012
/// <summary>
/// The simulator configuration. Every value is required; a missing or invalid one stops the run naming its key.
/// </summary>
/// <param name="BaseUrl">The API base URL</param>
/// <param name="Async">True to send Prefer: respond-async</param>
/// <param name="RequestsPerLoop">The number of requests each loop sends</param>
/// <param name="Profiles">The loop groups</param>
/// <param name="DrainTimeout">How long to wait for the queue to drain in async mode</param>
/// <param name="DrainPollInterval">How often to count the stored sales while draining</param>
/// <param name="RequestTimeout">How long any API call may take before it counts as unanswered</param>
/// <param name="AdminEmail">The e-mail of the administrator the API seeds, used to log in</param>
/// <param name="AdminPassword">The password of the administrator the API seeds</param>
public sealed record SimulatorSettings(
    Uri BaseUrl,
    bool Async,
    int RequestsPerLoop,
    IReadOnlyList<LoadProfile> Profiles,
    TimeSpan DrainTimeout,
    TimeSpan DrainPollInterval,
    TimeSpan RequestTimeout,
    string AdminEmail,
    string AdminPassword)
{
    // Work item: TASK-040 (FEAT-006), BUG-012
    /// <summary>
    /// Reads the settings, throwing <see cref="InvalidOperationException"/> naming the first missing or invalid key.
    /// </summary>
    /// <param name="configuration">The simulator configuration</param>
    /// <returns>The validated settings</returns>
    public static SimulatorSettings FromConfiguration(IConfiguration configuration)
    {
        var baseUrlText = Required(configuration, "Simulator:BaseUrl");
        if (!Uri.TryCreate(baseUrlText, UriKind.Absolute, out var baseUrl))
            throw new InvalidOperationException($"Simulator:BaseUrl must be an absolute URL; got \"{baseUrlText}\".");

        var mode = Required(configuration, "Simulator:Mode");
        var isAsync = mode.ToLowerInvariant() switch
        {
            "sync" => false,
            "async" => true,
            _ => throw new InvalidOperationException($"Simulator:Mode must be \"sync\" or \"async\"; got \"{mode}\".")
        };

        var profiles = configuration.GetSection("Simulator:Profiles").GetChildren()
            .Select(section => new LoadProfile(
                Required(configuration, $"Simulator:Profiles:{section.Key}:Name"),
                Integer(configuration, $"Simulator:Profiles:{section.Key}:Loops", minimum: 1),
                Integer(configuration, $"Simulator:Profiles:{section.Key}:PauseMilliseconds", minimum: 0)))
            .ToList();
        if (profiles.Count == 0)
            throw new InvalidOperationException("Simulator:Profiles must list at least one profile.");

        return new SimulatorSettings(
            baseUrl,
            isAsync,
            Integer(configuration, "Simulator:RequestsPerLoop", minimum: 1),
            profiles,
            PositiveTimeSpan(configuration, "Simulator:DrainTimeout"),
            PositiveTimeSpan(configuration, "Simulator:DrainPollInterval"),
            PositiveTimeSpan(configuration, "Simulator:RequestTimeout"),
            Required(configuration, "Seed:Admin:Email"),
            Required(configuration, "Seed:Admin:Password"));
    }

    private static int Integer(IConfiguration configuration, string key, int minimum)
    {
        var text = Required(configuration, key);
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < minimum)
            throw new InvalidOperationException($"{key} must be an integer of at least {minimum}; got \"{text}\".");

        return value;
    }

    private static TimeSpan PositiveTimeSpan(IConfiguration configuration, string key)
    {
        var text = Required(configuration, key);
        if (!TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var value) || value <= TimeSpan.Zero)
            throw new InvalidOperationException($"{key} must be a positive time span such as \"00:00:01\"; got \"{text}\".");

        return value;
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} is not configured. Set it in appsettings.json or pass --{key}=<value>.");

        return value;
    }
}
