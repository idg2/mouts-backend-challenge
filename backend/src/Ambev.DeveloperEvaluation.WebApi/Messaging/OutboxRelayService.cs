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

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int dispatched;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var relay = scope.ServiceProvider.GetRequiredService<IOutboxRelay>();
                    dispatched = await relay.DispatchPendingAsync(_settings.BatchSize, stoppingToken);
                }

                // A full batch means more rows may be waiting, so the next cycle starts at once.
                if (dispatched < _settings.BatchSize)
                    await Task.Delay(_settings.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox relay cycle failed; retrying after the polling interval");
                try
                {
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
