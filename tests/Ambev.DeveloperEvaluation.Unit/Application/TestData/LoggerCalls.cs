using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

// Work item: TASK-034 (FEAT-016)
/// <summary>
/// Reads the entries an NSubstitute <see cref="ILogger"/> received. The <c>LogInformation</c>-style extensions call
/// the generic <see cref="ILogger.Log{TState}"/>, which <c>Received()</c> cannot match with <c>Arg.Any</c>, so the
/// calls are read back instead.
/// </summary>
public static class LoggerCalls
{
    /// <summary>
    /// Returns the received entries in call order.
    /// </summary>
    public static IReadOnlyList<LoggedEntry> Entries(this ILogger logger) =>
        logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments())
            .Select(arguments => new LoggedEntry(
                (LogLevel)arguments[0]!,
                arguments[2]?.ToString() ?? string.Empty,
                arguments[3] as Exception,
                arguments[2] as IReadOnlyList<KeyValuePair<string, object?>> ?? []))
            .ToList();
}

// Work item: TASK-034 (FEAT-016)
/// <summary>
/// One entry received by a substitute logger: level, rendered message, exception, and structured properties.
/// </summary>
public sealed record LoggedEntry(
    LogLevel Level,
    string Message,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> Properties);
