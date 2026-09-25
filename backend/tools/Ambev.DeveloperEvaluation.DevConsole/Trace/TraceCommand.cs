using System.Globalization;
using System.Text.RegularExpressions;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-045 (FEAT-017), TASK-056 (FEAT-017)
/// <summary>
/// The trace command: picks the scenario, wipes the databases, hosts the API in this process with the console sink
/// installed, logs in as the seeded administrator, runs the scenario, and prints which keys were seen.
/// </summary>
public static class TraceCommand
{
    // Work item: TASK-056 (FEAT-017)
    private static readonly string[] SharedWithApi =
    [
        "ConnectionStrings:DefaultConnection",
        "ConnectionStrings:MessageBus",
        "Seed:Admin:Email",
        "Seed:Admin:Password"
    ];

    // Work item: TASK-056 (FEAT-017)
    private static readonly Regex StepKey = new(@"\b[A-Z]{3}-[A-Z]{3}-\d{2}\b", RegexOptions.Compiled);

    // Work item: TASK-045 (FEAT-017), TASK-056 (FEAT-017), TASK-057 (FEAT-017), TASK-059 (FEAT-017)
    /// <summary>
    /// Runs the command and returns the process exit code.
    /// </summary>
    /// <param name="parsed">The parsed command line</param>
    /// <param name="configuration">The merged configuration</param>
    /// <returns>0 on success, 1 on a setup failure or when a scenario failed, 2 on a bad scenario name</returns>
    public static async Task<int> RunAsync(ParsedCommandLine parsed, IConfiguration configuration)
    {
#if !DEBUG
        Console.Error.WriteLine("The trace command needs a Debug build: the StepTrace calls are compiled out in Release.");
        await Task.CompletedTask;
        return 1;
#else
        var scenarios = ChooseScenarios(parsed.Scenario);
        if (scenarios is null)
            return 2;

        // Every required setting is read before anything is dropped: a missing one is a setup error.
        var hostSettings = HostSettings(configuration, parsed.ConfigurationArgs);
        var waitTimeout = WaitTimeout(configuration);
        var adminEmail = Required(configuration, "Seed:Admin:Email");
        var adminPassword = Required(configuration, "Seed:Admin:Password");
        var maintenanceDatabase = Required(configuration, "Trace:MaintenanceDatabase");

        int wipe;
        try
        {
            wipe = await DatabaseWipe.RunAsync(configuration, maintenanceDatabase, Console.Out, Console.Error, () => parsed.Yes || Confirm());
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            Console.Error.WriteLine(OneLine("The databases could not be wiped", exception));
            return 1;
        }

        if (wipe != 0)
            return wipe;

        var waiter = new StepWaiter();
        var sink = new ConsoleSink(Console.Out, waiter);
        StepTrace.Sink = sink.Handle;
        try
        {
            var runId = Guid.NewGuid().ToString("N")[..8];
            await using var host = new TraceHost(hostSettings);
            HttpClient client;
            string adminToken;
            try
            {
                client = host.CreateClient();
                sink.HostReady = true;
                adminToken = await new ScenarioContext(client, string.Empty, waiter, waitTimeout, runId, Console.Out, adminEmail, adminPassword)
                    .LoginAsync(adminEmail, adminPassword);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(OneLine("The API could not start", exception));
                return 1;
            }

            using var disposeClient = client;
            if (adminToken.Length == 0)
            {
                Console.Error.WriteLine("The administrator login failed; the seed did not run or the credentials differ.");
                return 1;
            }

            var context = new ScenarioContext(client, adminToken, waiter, waitTimeout, runId, Console.Out, adminEmail, adminPassword);
            var allSucceeded = await RunScenariosAsync(scenarios, context);
            PrintSummary(sink, scenarios.Count == ScenarioCatalog.All.Count);
            return allSucceeded ? 0 : 1;
        }
        finally
        {
            StepTrace.Sink = null;
        }
#endif
    }

    // Work item: TASK-057 (FEAT-017)
    /// <summary>
    /// Runs the scenarios in order, each under its banner. A scenario that throws gets one failure line and the run
    /// goes on with the next one, so a single failure does not hide the trace of the others.
    /// </summary>
    /// <param name="scenarios">The scenarios to run</param>
    /// <param name="context">The shared context; its output receives the banners and the failure lines</param>
    /// <returns>True when every scenario finished without an exception</returns>
    public static async Task<bool> RunScenariosAsync(IReadOnlyList<IScenario> scenarios, ScenarioContext context)
    {
        var allSucceeded = true;
        foreach (var scenario in scenarios)
        {
            context.Out.WriteLine();
            context.Out.WriteLine($"===== {scenario.Name}: {scenario.Description} (run {context.RunId})");
            try
            {
                await scenario.RunAsync(context);
            }
            catch (Exception exception)
            {
                allSucceeded = false;
                context.Out.WriteLine($"!!! scenario {scenario.Name} failed: {exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}");
            }
        }

        return allSucceeded;
    }

    // Work item: TASK-056 (FEAT-017)
    /// <summary>
    /// Builds the host configuration of the in-process API: every setting given on the command line, then the
    /// console's resolved connection strings and administrator, so the API uses exactly the databases the console
    /// wiped and the administrator it logs in as, plus the application log level of the trace.
    /// </summary>
    /// <param name="configuration">The merged configuration</param>
    /// <param name="configurationArgs">The command-line settings, as --Key=value</param>
    /// <returns>The host settings</returns>
    public static IReadOnlyDictionary<string, string?> HostSettings(IConfiguration configuration, string[] configurationArgs)
    {
        var settings = new ConfigurationBuilder().AddCommandLine(configurationArgs).Build()
            .AsEnumerable()
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var key in SharedWithApi)
        {
            if (configuration[key] is { } value)
                settings[key] = value;
        }

        settings["Serilog:MinimumLevel:Default"] = Required(configuration, "Trace:AppLogMinimumLevel");
        return settings;
    }

    // Work item: TASK-056 (FEAT-017)
    /// <summary>
    /// Reads how long a scenario waits for a traced key.
    /// </summary>
    /// <param name="configuration">The merged configuration</param>
    /// <returns>The positive timeout</returns>
    public static TimeSpan WaitTimeout(IConfiguration configuration)
    {
        const string key = "Trace:WaitTimeout";
        return TimeSpan.TryParse(Required(configuration, key), CultureInfo.InvariantCulture, out var timeout) && timeout > TimeSpan.Zero
            ? timeout
            : throw new InvalidOperationException($"{key} must be a positive time span, such as 00:00:30.");
    }

    // Work item: TASK-056 (FEAT-017)
    private static string Required(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"{key} is not configured.");

#if DEBUG
    // Work item: TASK-056 (FEAT-017)
    private static string OneLine(string what, Exception exception) =>
        $"{what}: {exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}";

    // Work item: TASK-056 (FEAT-017)
    private static IReadOnlyList<IScenario>? ChooseScenarios(string? name)
    {
        if (name is null)
        {
            Console.WriteLine("Scenarios:");
            for (var index = 0; index < ScenarioCatalog.All.Count; index++)
                Console.WriteLine($"  {index + 1,2}. {ScenarioCatalog.All[index].Name,-12} {ScenarioCatalog.All[index].Description}");
            Console.WriteLine($"  {ScenarioCatalog.All.Count + 1,2}. all");
            Console.Write("Scenario (number or name): ");
            name = Console.ReadLine()?.Trim() ?? string.Empty;
            if (int.TryParse(name, out var number) && number >= 1 && number <= ScenarioCatalog.All.Count + 1)
                name = number == ScenarioCatalog.All.Count + 1 ? "all" : ScenarioCatalog.All[number - 1].Name;
        }

        if (name == "all")
            return ScenarioCatalog.All;

        var chosen = ScenarioCatalog.All.FirstOrDefault(scenario => scenario.Name == name);
        if (chosen is not null)
            return [chosen];

        Console.Error.WriteLine($"Unknown scenario \"{name}\". Known: {string.Join(", ", ScenarioCatalog.All.Select(scenario => scenario.Name))}, all.");
        return null;
    }

    // Work item: TASK-056 (FEAT-017)
    private static bool Confirm()
    {
        Console.Write("Continue? (y/n) ");
        return Console.ReadLine()?.Trim().ToLowerInvariant() is "y" or "yes" or "s" or "sim";
    }

    // Work item: TASK-056 (FEAT-017)
    private static void PrintSummary(ConsoleSink sink, bool fullRun)
    {
        var seen = sink.KeysSeen.Order().ToList();
        Console.WriteLine();
        Console.WriteLine($"===== {seen.Count} distinct keys seen: {string.Join(' ', seen)}");
        if (!fullRun)
            return;

        var docs = Path.Combine(ConsoleConfiguration.FindSolutionDirectory(), "docs");
        var documented = Directory.GetFiles(docs, "*.md")
            .Where(path => Path.GetFileName(path) != "TEMPLATE.md")
            .SelectMany(path => StepKey.Matches(File.ReadAllText(path)).Select(match => match.Value))
            .ToHashSet();
        var missing = documented.Except(seen).Order().ToList();
        var expected = missing.Where(ScenarioCatalog.ExpectedMisses.Contains).ToList();
        var unexpected = missing.Except(expected).ToList();
        Console.WriteLine($"===== documented keys not seen: {missing.Count} ({expected.Count} expected: {string.Join(' ', expected)})");
        Console.WriteLine(unexpected.Count == 0
            ? "===== no unexpected misses"
            : $"===== UNEXPECTED misses: {string.Join(' ', unexpected)}");
    }
#endif
}
