using System.Diagnostics;
using Ambev.DeveloperEvaluation.LoadSimulator;
using Microsoft.Extensions.Configuration;

// Work item: TASK-040 (FEAT-006)
// Load simulator: posts sales from concurrent loops in sync or async mode and prints latency, throughput, and,
// in async mode, how long the queue takes to drain.
try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddCommandLine(args)
        .Build();
    var settings = SimulatorSettings.FromConfiguration(configuration);

    using var http = new HttpClient { BaseAddress = settings.BaseUrl, Timeout = settings.RequestTimeout };
    var api = new ApiClient(http);
    var runId = Guid.NewGuid().ToString("N")[..8];
    var loops = settings.Profiles.Sum(profile => profile.Loops);
    Console.WriteLine(
        $"Run {runId}: {(settings.Async ? "async" : "sync")} mode, {loops} concurrent loops x " +
        $"{settings.RequestsPerLoop} requests against {settings.BaseUrl}, request timeout {settings.RequestTimeout}");

    await api.AuthenticateAsNewManagerAsync(runId);
    var sale = await api.CreateSaleBodyAsync(runId);
    var baseline = await api.CountSalesAsync();

    var load = Stopwatch.StartNew();
    var samples = await LoadRunner.RunAsync(api, settings, sale);
    load.Stop();
    LoadReport.Print(samples, load.Elapsed, Console.Out);

    if (settings.Async)
    {
        var accepted = samples.Count(sample => sample.Outcome == "202");
        var unanswered = samples.Count(sample => !int.TryParse(sample.Outcome, out _));
        Console.WriteLine(
            $"Unanswered requests (timeout or network error): {unanswered}. They may still have been queued, so the " +
            "drain target below can be reached before every accepted sale is stored.");
        var target = baseline + accepted;
        var drain = Stopwatch.StartNew();
        var stored = await api.CountSalesAsync();
        while (stored < target && drain.Elapsed < settings.DrainTimeout)
        {
            await Task.Delay(settings.DrainPollInterval);
            stored = await api.CountSalesAsync();
        }

        Console.WriteLine(stored >= target
            ? $"Drain: all {accepted} accepted sales stored {drain.Elapsed.TotalSeconds:F1} s after the load ended " +
              $"({(load.Elapsed + drain.Elapsed).TotalSeconds:F1} s after it started)."
            : $"Drain: {target - stored} of {accepted} accepted sales not stored after {settings.DrainTimeout}; " +
              "they are still queued or in the error queue.");
    }

    Console.WriteLine("Sales created by anything else during the run distort the drain count.");
    return 0;
}
catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or TaskCanceledException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
