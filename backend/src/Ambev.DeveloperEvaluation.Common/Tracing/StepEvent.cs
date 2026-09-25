namespace Ambev.DeveloperEvaluation.Common.Tracing;

// Work item: TASK-042 (FEAT-017), TASK-043 (FEAT-017)
/// <summary>
/// A shared line of generic code (the transaction behavior) whose topic key depends on the request type.
/// </summary>
public enum SharedPoint
{
    /// <summary>The transaction begins.</summary>
    TransactionBegin,

    /// <summary>The transaction commits.</summary>
    TransactionCommit,

    /// <summary>The transaction rolls back.</summary>
    TransactionRollback
}

// Work item: TASK-042 (FEAT-017)
/// <summary>
/// One traced step: when, on which thread, which documented keys, and the values that decided the path.
/// </summary>
/// <param name="At">The local time of the call</param>
/// <param name="ThreadId">The managed thread id of the caller</param>
/// <param name="Key">The topic step key, or null when a generic line has no topic key for the request type</param>
/// <param name="SharedKey">The CMN step key the same line also documents, or null</param>
/// <param name="Title">The diagram label without the key</param>
/// <param name="Values">The formatted name and value pairs</param>
/// <param name="File">The full path of the source file of the call, as compiled</param>
/// <param name="Line">The line of the call</param>
public sealed record StepEvent(
    DateTime At,
    int ThreadId,
    string? Key,
    string? SharedKey,
    string Title,
    IReadOnlyList<(string Name, string Value)> Values,
    string File,
    int Line)
{
    /// <summary>
    /// Gets the non-null keys joined by one space, the topic key first.
    /// </summary>
    public string Keys => Key is null ? SharedKey ?? string.Empty : SharedKey is null ? Key : $"{Key} {SharedKey}";

    /// <summary>
    /// Gets the file name without its directory.
    /// </summary>
    public string FileName => Path.GetFileName(File);
}
