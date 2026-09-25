using Ambev.DeveloperEvaluation.DevConsole;
using Ambev.DeveloperEvaluation.DevConsole.Load;
using Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-044 (FEAT-017), TASK-045 (FEAT-017)
// Developer console: t runs a traced scenario against the API hosted in this process; l runs the load simulator
// against a running API.
try
{
    var parsed = CommandLine.Parse(args);
    var command = parsed.Command;
    if (command is null)
    {
        Console.Write("Command: [t]race a scenario or [l]oad test? ");
        command = Console.ReadLine()?.Trim().ToLowerInvariant();
    }

    var configuration = ConsoleConfiguration.Build(parsed.ConfigurationArgs);
    return command switch
    {
        "l" => await LoadCommand.RunAsync(configuration),
        "t" => await TraceCommand.RunAsync(parsed, configuration),
        _ => Usage()
    };
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: [t [scenario]|l] [--yes] [--Key=value ...]");
    return 2;
}
