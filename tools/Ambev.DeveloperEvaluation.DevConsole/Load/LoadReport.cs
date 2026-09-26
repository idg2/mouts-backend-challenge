namespace Ambev.DeveloperEvaluation.DevConsole.Load;

// Work item: TASK-040 (FEAT-006), TASK-044 (FEAT-017)
/// <summary>
/// Prints request counts per outcome, throughput, and latency percentiles per profile and overall.
/// </summary>
public static class LoadReport
{
    /// <summary>
    /// Prints one line per profile and a TOTAL line. Profiles run at the same time and end at different moments, so
    /// throughput is only printed for the whole run.
    /// </summary>
    /// <param name="samples">The collected samples</param>
    /// <param name="elapsed">The wall-clock duration of the load</param>
    /// <param name="output">Where to print</param>
    public static void Print(IReadOnlyList<RequestSample> samples, TimeSpan elapsed, TextWriter output)
    {
        foreach (var profile in samples.GroupBy(sample => sample.Profile).OrderBy(group => group.Key))
            PrintLine(profile.Key, profile.ToList(), throughput: null, output);

        PrintLine("TOTAL", samples, samples.Count / elapsed.TotalSeconds, output);
    }

    private static void PrintLine(string name, IReadOnlyList<RequestSample> samples, double? throughput, TextWriter output)
    {
        var sorted = samples.Select(sample => sample.Milliseconds).Order().ToArray();
        var outcomes = string.Join(", ", samples
            .GroupBy(sample => sample.Outcome)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}={group.Count()}"));

        output.WriteLine(
            $"{name}: {samples.Count} requests, " +
            (throughput is null ? "" : $"{throughput:F1} req/s over the whole run, ") +
            $"p50={Percentile(sorted, 50):F0} ms, p95={Percentile(sorted, 95):F0} ms, p99={Percentile(sorted, 99):F0} ms, " +
            $"outcomes: {outcomes}");
    }

    // Nearest-rank percentile over ascending values.
    private static double Percentile(double[] sorted, int percentile) =>
        sorted.Length == 0 ? 0 : sorted[Math.Max(0, (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1)];
}
