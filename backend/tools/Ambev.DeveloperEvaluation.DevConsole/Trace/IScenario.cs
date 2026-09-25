namespace Ambev.DeveloperEvaluation.DevConsole.Trace;

// Work item: TASK-056 (FEAT-017)
/// <summary>
/// One traced scenario: a named sequence of requests and waits against the API hosted in this process.
/// </summary>
public interface IScenario
{
    /// <summary>Gets the name typed on the command line or chosen in the menu.</summary>
    string Name { get; }

    /// <summary>Gets the one-line description shown in the menu.</summary>
    string Description { get; }

    /// <summary>Runs the scenario.</summary>
    /// <param name="context">The client, the token, the waiter, and the printer</param>
    Task RunAsync(ScenarioContext context);
}
