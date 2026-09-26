using Microsoft.Extensions.Configuration;

namespace Ambev.DeveloperEvaluation.DevConsole;

// Work item: BUG-012, TASK-044 (FEAT-017), TASK-056 (FEAT-017)
/// <summary>
/// Builds the console configuration from the sources the WebApi reads, in the WebApi's order (appsettings,
/// appsettings.Development, the WebApi user secrets, environment variables), so the administrator the console logs in
/// as is the one the API seeds and the connection strings are the API's; then the console's own appsettings, then the
/// command line.
/// </summary>
public static class ConsoleConfiguration
{
    // Work item: BUG-012, TASK-044 (FEAT-017), TASK-056 (FEAT-017)
    /// <summary>
    /// Builds the merged configuration.
    /// </summary>
    /// <param name="configurationArgs">The command-line arguments left after the command, the scenario, and --yes</param>
    /// <returns>The configuration</returns>
    public static IConfiguration Build(string[] configurationArgs)
    {
        var webApiDirectory = FindWebApiDirectory();
        return new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(webApiDirectory, "appsettings.Development.json"), optional: true)
            .AddUserSecrets(typeof(Ambev.DeveloperEvaluation.WebApi.Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddCommandLine(configurationArgs)
            .Build();
    }

    /// <summary>
    /// Walks up from the build output to the solution and returns the WebApi project directory.
    /// </summary>
    /// <returns>The absolute WebApi project directory</returns>
    public static string FindWebApiDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ambev.DeveloperEvaluation.sln")))
            directory = directory.Parent;

        return directory is null
            ? throw new InvalidOperationException("Could not locate Ambev.DeveloperEvaluation.sln from the console output directory")
            : Path.Combine(directory.FullName, "src", "Ambev.DeveloperEvaluation.WebApi");
    }

    // Work item: TASK-056 (FEAT-017)
    /// <summary>
    /// Walks up from the build output and returns the directory that holds the solution file.
    /// </summary>
    /// <returns>The absolute solution directory</returns>
    public static string FindSolutionDirectory() => Path.GetDirectoryName(Path.GetDirectoryName(FindWebApiDirectory())!)!;
}
