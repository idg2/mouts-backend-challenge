using Ambev.DeveloperEvaluation.ORM.Outbox;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging;

// Work item: TASK-031 (FEAT-004)
/// <summary>
/// Contains unit tests for the <see cref="OutboxRelayService"/> loop: when the next cycle starts at once and when it
/// waits. The polling interval is one hour, so any cycle that runs during a test did not wait for it.
/// </summary>
public class OutboxRelayServiceTests
{
    private const int BatchSize = 2;

    /// <summary>
    /// Tests that full batches are followed by another cycle at once, until a partial batch makes the loop wait.
    /// </summary>
    [Fact(DisplayName = "Given full batches When cycles end Then the next cycle starts without waiting")]
    public async Task Given_FullBatches_When_CyclesEnd_Then_NextCycleStartsWithoutWaiting()
    {
        // Arrange
        var relay = new ScriptedRelay(BatchSize, BatchSize, 1, BatchSize);

        // Act
        var cycles = await RunAsync(relay, expectedCycles: 3);

        // Assert
        cycles.Should().Be(3);
    }

    /// <summary>
    /// Tests that a partial batch makes the loop wait the polling interval before the next cycle.
    /// </summary>
    [Fact(DisplayName = "Given a partial batch When the cycle ends Then the next cycle waits the polling interval")]
    public async Task Given_PartialBatch_When_CycleEnds_Then_NextCycleWaits()
    {
        // Arrange
        var relay = new ScriptedRelay(1, BatchSize);

        // Act
        var cycles = await RunAsync(relay, expectedCycles: 1);

        // Assert
        cycles.Should().Be(1);
    }

    /// <summary>
    /// Tests that a failed cycle makes the loop wait the polling interval instead of retrying in a hot loop, and that
    /// the service keeps running.
    /// </summary>
    [Fact(DisplayName = "Given a failed cycle When it ends Then the next cycle waits the polling interval")]
    public async Task Given_FailedCycle_When_ItEnds_Then_NextCycleWaits()
    {
        // Arrange
        var relay = new ScriptedRelay(new InvalidOperationException("bus down"), BatchSize);

        // Act
        var cycles = await RunAsync(relay, expectedCycles: 1);

        // Assert
        cycles.Should().Be(1);
    }

    private static async Task<int> RunAsync(ScriptedRelay relay, int expectedCycles)
    {
        await using var provider = new ServiceCollection()
            .AddScoped<IOutboxRelay>(_ => relay)
            .BuildServiceProvider();
        var service = new OutboxRelayService(
            provider.GetRequiredService<IServiceScopeFactory>(), Settings(), NullLogger<OutboxRelayService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await relay.WaitForCyclesAsync(expectedCycles, TimeSpan.FromSeconds(5));
        // Leave room for a cycle that should not run.
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await service.StopAsync(CancellationToken.None);

        return relay.Cycles;
    }

    private static MessagingSettings Settings() => MessagingSettings.FromConfiguration(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [MessagingSettings.ConnectionStringKey] = "mongodb://localhost:27017/bus_db",
            [MessagingSettings.InputQueueKey] = "sales-intake",
            [MessagingSettings.WorkersKey] = "1",
            [MessagingSettings.MaxParallelismKey] = "1",
            [MessagingSettings.PollingIntervalKey] = "01:00:00",
            [MessagingSettings.BatchSizeKey] = BatchSize.ToString()
        }).Build());

    // Work item: TASK-031 (FEAT-004)
    /// <summary>
    /// A relay that answers each cycle from a script (a dispatched count or an exception) and then with 0.
    /// </summary>
    private sealed class ScriptedRelay : IOutboxRelay
    {
        private readonly Queue<object> _script;
        private readonly SemaphoreSlim _cycleRan = new(0);
        private int _cycles;

        /// <summary>
        /// Initializes the relay with its answers, in cycle order.
        /// </summary>
        public ScriptedRelay(params object[] script)
        {
            _script = new Queue<object>(script);
        }

        /// <summary>
        /// Gets the number of cycles run so far.
        /// </summary>
        public int Cycles => Volatile.Read(ref _cycles);

        /// <inheritdoc />
        public Task<int> DispatchPendingAsync(int batchSize, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _cycles);
            _cycleRan.Release();
            var answer = _script.Count > 0 ? _script.Dequeue() : 0;
            return answer is Exception exception ? Task.FromException<int>(exception) : Task.FromResult((int)answer);
        }

        /// <summary>
        /// Waits until the given number of cycles ran, or the timeout elapsed.
        /// </summary>
        public async Task WaitForCyclesAsync(int count, TimeSpan timeout)
        {
            for (var cycle = 0; cycle < count; cycle++)
            {
                if (!await _cycleRan.WaitAsync(timeout))
                    return;
            }
        }
    }
}
