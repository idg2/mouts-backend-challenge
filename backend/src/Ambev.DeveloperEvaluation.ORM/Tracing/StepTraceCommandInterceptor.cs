#if DEBUG
using System.Data.Common;
using Ambev.DeveloperEvaluation.Common.Tracing;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ambev.DeveloperEvaluation.ORM.Tracing;

// Work item: TASK-047 (FEAT-017)
/// <summary>
/// Traces CMN-PIP-11 for every SQL command DefaultContext runs: the kind, the first line of the statement, the
/// parameter count, the elapsed time, and the affected rows. Parameter values never print. Debug builds only.
/// </summary>
public sealed class StepTraceCommandInterceptor : DbCommandInterceptor
{
    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Trace(command, eventData, rows: null);
        return base.ReaderExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Trace(command, eventData, rows: null);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Trace(command, eventData, result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Trace(command, eventData, result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Trace(command, eventData, rows: null);
        return base.ScalarExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Trace(command, eventData, rows: null);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    // Work item: TASK-047 (FEAT-017), TASK-059 (FEAT-017)
    private static void Trace(DbCommand command, CommandExecutedEventData eventData, int? rows)
    {
        if (StepTrace.Sink is null)
            return;

        StepTrace.Step("CMN-PIP-11", "Repository reads or writes DefaultContext",
            [("kind", eventData.ExecuteMethod), ("sql", FirstLine(command.CommandText)), ("parameters", command.Parameters.Count),
             ("elapsedMs", eventData.Duration.TotalMilliseconds), ("rows", rows)]);
    }

    private static string FirstLine(string sql)
    {
        var line = sql.AsSpan().Trim();
        var end = line.IndexOfAny('\r', '\n');
        if (end >= 0)
            line = line[..end];

        return line.Length > 120 ? string.Concat(line[..120], "…") : line.ToString();
    }
}
#endif
