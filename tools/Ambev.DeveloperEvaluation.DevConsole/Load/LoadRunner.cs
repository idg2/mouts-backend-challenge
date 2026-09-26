using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ambev.DeveloperEvaluation.DevConsole.Load;

// Work item: TASK-040 (FEAT-006), TASK-044 (FEAT-017)
/// <summary>
/// One timed request: the profile that sent it, its outcome (status code or exception name), and its duration.
/// </summary>
/// <param name="Profile">The profile name</param>
/// <param name="Outcome">The status code, or the exception type name when no response came back</param>
/// <param name="Milliseconds">The request duration</param>
public sealed record RequestSample(string Profile, string Outcome, double Milliseconds);

// Work item: TASK-040 (FEAT-006), TASK-044 (FEAT-017)
/// <summary>
/// Runs every loop of every profile at the same time and collects one sample per request.
/// </summary>
public static class LoadRunner
{
    /// <summary>
    /// Starts all loops and waits for all of them. Loops are tasks, so concurrency equals the total number of loops.
    /// </summary>
    /// <param name="api">The API client</param>
    /// <param name="settings">The simulator settings</param>
    /// <param name="sale">The sale body to post</param>
    /// <returns>One sample per request</returns>
    public static async Task<IReadOnlyList<RequestSample>> RunAsync(ApiClient api, SimulatorSettings settings, object sale)
    {
        var samples = new ConcurrentBag<RequestSample>();
        var loops = settings.Profiles
            .SelectMany(profile => Enumerable.Range(0, profile.Loops)
                .Select(_ => RunLoopAsync(api, settings, profile, sale, samples)))
            .ToList();

        await Task.WhenAll(loops);
        return samples.ToList();
    }

    private static async Task RunLoopAsync(
        ApiClient api, SimulatorSettings settings, LoadProfile profile, object sale, ConcurrentBag<RequestSample> samples)
    {
        // Let every loop start before the first request completes.
        await Task.Yield();

        var sent = 0;
        while (sent < settings.RequestsPerLoop)
        {
            var stopwatch = Stopwatch.StartNew();
            string outcome;
            try
            {
                outcome = (await api.PostSaleAsync(sale, settings.Async)).ToString();
            }
            catch (Exception exception)
            {
                // Any failure without a response is a sample, never the end of the run.
                outcome = exception.GetType().Name;
            }

            stopwatch.Stop();
            samples.Add(new RequestSample(profile.Name, outcome, stopwatch.Elapsed.TotalMilliseconds));
            sent++;

            if (profile.PauseMilliseconds > 0)
                await Task.Delay(profile.PauseMilliseconds);
        }
    }
}
