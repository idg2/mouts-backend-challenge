using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.ORM.Outbox;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

// Work item: TASK-031 (FEAT-004)
/// <summary>
/// Runs <see cref="OutboxRelay"/> cycles until the host stops. After a full batch the next cycle starts at once, since
/// more rows may be waiting; after a partial batch, or a failed cycle, it waits <see cref="MessagingSettings.PollingInterval"/>.
/// A failed cycle is logged and the loop goes on, so a database or bus outage never stops the API.
/// </summary>
public class OutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessagingSettings _settings;
    private readonly ILogger<OutboxRelayService> _logger;

    /// <summary>
    /// Initializes a new instance of OutboxRelayService
    /// </summary>
    /// <param name="scopeFactory">Creates the scope each cycle's DbContext lives in</param>
    /// <param name="settings">The polling interval and batch size</param>
    /// <param name="logger">The logger</param>
    public OutboxRelayService(IServiceScopeFactory scopeFactory, MessagingSettings settings, ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    // Work item: TASK-054 (FEAT-017)
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StepTrace.Step("SAL-RLY-01", "Host starts the relay after the bus",
            [("batchSize", _settings.BatchSize), ("pollingInterval", _settings.PollingInterval)]);
        while (!stoppingToken.IsCancellationRequested)
        {
            StepTrace.Step("SAL-RLY-02", "Stopping?", [("stopping", stoppingToken.IsCancellationRequested)]);
            try
            {
                int dispatched;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var relay = scope.ServiceProvider.GetRequiredService<IOutboxRelay>();
                    StepTrace.Step("SAL-RLY-03", "Create a DI scope and resolve the relay", [("relay", relay.GetType().Name)]);
                    StepTrace.Step("SAL-RLY-04", "Run one dispatch cycle, see SAL-DSP", [("batchSize", _settings.BatchSize)]);
                    dispatched = await relay.DispatchPendingAsync(_settings.BatchSize, stoppingToken);
                }

                StepTrace.Step("SAL-RLY-05", "Full batch?",
                    [("dispatched", dispatched), ("batchSize", _settings.BatchSize), ("full", dispatched >= _settings.BatchSize)]);
                // A full batch means more rows may be waiting, so the next cycle starts at once.
                if (dispatched < _settings.BatchSize)
                {
                    StepTrace.Step("SAL-RLY-06", "Wait Outbox:PollingInterval", [("pollingInterval", _settings.PollingInterval), ("afterFailure", false)]);
                    await Task.Delay(_settings.PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox relay cycle failed; retrying after the polling interval");
                StepTrace.Step("SAL-RLY-07", "Log the failure", [("error", exception)]);
                try
                {
                    StepTrace.Step("SAL-RLY-06", "Wait Outbox:PollingInterval", [("pollingInterval", _settings.PollingInterval), ("afterFailure", true)]);
                    await Task.Delay(_settings.PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
