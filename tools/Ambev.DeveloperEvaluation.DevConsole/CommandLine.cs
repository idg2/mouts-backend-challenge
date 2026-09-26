namespace Ambev.DeveloperEvaluation.DevConsole;

// Work item: TASK-044 (FEAT-017)
/// <summary>
/// What the console consumed from the command line: the command, the scenario, and the wipe confirmation.
/// Everything else goes to the configuration builder.
/// </summary>
/// <param name="Command">"t" or "l", or null when the console must ask</param>
/// <param name="Scenario">The scenario name after "t", or null when the console must ask</param>
/// <param name="Yes">True when --yes was given</param>
/// <param name="ConfigurationArgs">The remaining arguments</param>
public sealed record ParsedCommandLine(string? Command, string? Scenario, bool Yes, string[] ConfigurationArgs);

// Work item: TASK-044 (FEAT-017)
/// <summary>
/// Parses the console command line: <c>[t [scenario]|l] [--yes] [--Key=value ...]</c>.
/// </summary>
public static class CommandLine
{
    /// <summary>
    /// Parses the arguments.
    /// </summary>
    /// <param name="args">The process arguments</param>
    /// <returns>The parsed command line</returns>
    public static ParsedCommandLine Parse(string[] args)
    {
        string? command = null;
        string? scenario = null;
        var yes = false;
        var rest = new List<string>();

        foreach (var arg in args)
        {
            if (arg == "--yes")
                yes = true;
            else if (arg.StartsWith("--", StringComparison.Ordinal))
                rest.Add(arg);
            else if (command is null && arg is "t" or "l")
                command = arg;
            else if (command == "t" && scenario is null)
                scenario = arg;
            else
                throw new InvalidOperationException($"Unexpected argument \"{arg}\". Usage: [t [scenario]|l] [--yes] [--Key=value ...]");
        }

        return new ParsedCommandLine(command, scenario, yes, rest.ToArray());
    }
}
