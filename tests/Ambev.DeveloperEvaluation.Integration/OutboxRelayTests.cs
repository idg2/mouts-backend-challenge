using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-030 (FEAT-004)
/// <summary>
/// Contains integration tests for the <see cref="OutboxRelay"/> class. Each test empties the outbox first, since
/// the relay reads every pending row of the shared database.
/// </summary>
public class OutboxRelayTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public OutboxRelayTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that pending rows are published in sequence order with the row id as event id and marked processed.
    /// </summary>
    [Fact(DisplayName = "Given pending rows When dispatching Then publishes in sequence order and marks them processed")]
    public async Task Given_PendingRows_When_Dispatching_Then_PublishesInOrderAndMarksProcessed()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        var rows = await SeedAsync(new SaleDeleted(saleId), new SaleCancelled(saleId), new SaleDeleted(Guid.NewGuid()));
        var (publisher, published) = NewPublisher();

        // Act
        var dispatched = await DispatchAsync(publisher, batchSize: 10);

        // Assert
        Assert.Equal(3, dispatched);
        Assert.Equal(rows.Select(row => row.Id), published.Select(call => call.EventId));
        Assert.Equal(new SaleDeleted(saleId), published[0].Event);
        Assert.Equal(new SaleCancelled(saleId), published[1].Event);
        Assert.All(await ReadAsync(rows), row => Assert.NotNull(row.ProcessedAt));
    }

    /// <summary>
    /// Tests that one cycle dispatches at most the batch size, oldest first.
    /// </summary>
    [Fact(DisplayName = "Given more rows than the batch size When dispatching Then dispatches only the oldest batch")]
    public async Task Given_MoreRowsThanBatchSize_When_Dispatching_Then_DispatchesOnlyOldestBatch()
    {
        // Arrange
        var rows = await SeedAsync(new SaleDeleted(Guid.NewGuid()), new SaleDeleted(Guid.NewGuid()), new SaleDeleted(Guid.NewGuid()));
        var (publisher, published) = NewPublisher();

        // Act
        var dispatched = await DispatchAsync(publisher, batchSize: 2);

        // Assert
        Assert.Equal(2, dispatched);
        Assert.Equal(rows.Take(2).Select(row => row.Id), published.Select(call => call.EventId));
        Assert.Equal((await ReadAsync(rows)).Take(2).Select(row => row.Sequence), published.Select(call => call.Sequence));
        Assert.Null((await ReadAsync(rows))[2].ProcessedAt);
    }

    /// <summary>
    /// Tests that a failed publish stops the cycle at that row, leaving it and later rows pending, and that the next
    /// cycle resumes from that row in order without re-sending the processed one.
    /// </summary>
    [Fact(DisplayName = "Given publish fails on the second row When dispatching Then stops there and the next cycle resumes in order")]
    public async Task Given_PublishFailsOnSecondRow_When_Dispatching_Then_StopsThereAndNextCycleResumesInOrder()
    {
        // Arrange
        var rows = await SeedAsync(new SaleDeleted(Guid.NewGuid()), new SaleDeleted(Guid.NewGuid()), new SaleDeleted(Guid.NewGuid()));
        var (failing, failingCalls) = NewPublisher();
        failing.PublishAsync(Arg.Any<IIntegrationEvent>(), rows[1].Id, Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("bus down")));

        // Act
        var firstCycle = await DispatchAsync(failing, batchSize: 10);
        var (healthy, healthyCalls) = NewPublisher();
        var secondCycle = await DispatchAsync(healthy, batchSize: 10);

        // Assert
        Assert.Equal(1, firstCycle);
        Assert.Equal(new[] { rows[0].Id, rows[1].Id }, failingCalls.Select(call => call.EventId));
        Assert.Equal(2, secondCycle);
        Assert.Equal(new[] { rows[1].Id, rows[2].Id }, healthyCalls.Select(call => call.EventId));
        Assert.All(await ReadAsync(rows), row => Assert.NotNull(row.ProcessedAt));
    }

    /// <summary>
    /// Tests that a row whose type is not registered blocks the cycle like a failed publish and is never published.
    /// </summary>
    [Fact(DisplayName = "Given a row with an unknown type When dispatching Then stops there without publishing it")]
    public async Task Given_RowWithUnknownType_When_Dispatching_Then_StopsThereWithoutPublishing()
    {
        // Arrange
        await SeedAsync();
        var unknown = new OutboxMessage
        {
            Id = Guid.NewGuid(), Type = "System.IO.File", Payload = "{}", OccurredAt = DateTime.UtcNow
        };
        await using (var context = _fixture.CreateContext())
        {
            context.OutboxMessages.Add(unknown);
            await context.SaveChangesAsync();
        }

        var (publisher, published) = NewPublisher();

        // Act
        var dispatched = await DispatchAsync(publisher, batchSize: 10);

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Empty(published);
        Assert.Null((await ReadAsync([unknown]))[0].ProcessedAt);
    }

    private async Task<List<OutboxMessage>> SeedAsync(params IIntegrationEvent[] events)
    {
        await using var context = _fixture.CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
        var unitOfWork = new UnitOfWork(context);
        await unitOfWork.BeginTransactionAsync();
        var writer = new OutboxWriter(context, TimeProvider.System);
        foreach (var integrationEvent in events)
            await writer.EnqueueAsync(integrationEvent);
        await unitOfWork.CommitTransactionAsync();
        return await context.OutboxMessages.AsNoTracking().OrderBy(message => message.Sequence).ToListAsync();
    }

    private async Task<List<OutboxMessage>> ReadAsync(IEnumerable<OutboxMessage> rows)
    {
        var ids = rows.Select(row => row.Id).ToList();
        await using var context = _fixture.CreateContext();
        return await context.OutboxMessages.AsNoTracking()
            .Where(message => ids.Contains(message.Id))
            .OrderBy(message => message.Sequence)
            .ToListAsync();
    }

    private async Task<int> DispatchAsync(IEventPublisher publisher, int batchSize)
    {
        await using var context = _fixture.CreateContext();
        var relay = new OutboxRelay(context, publisher, TimeProvider.System, NullLogger<OutboxRelay>.Instance);
        return await relay.DispatchPendingAsync(batchSize, CancellationToken.None);
    }

    // Work item: TASK-076 (FEAT-003)
    private static (IEventPublisher Publisher, List<(IIntegrationEvent Event, Guid EventId, long Sequence)> Calls) NewPublisher()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var calls = new List<(IIntegrationEvent Event, Guid EventId, long Sequence)>();
        publisher.When(p => p.PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>()))
            .Do(call => calls.Add((call.ArgAt<IIntegrationEvent>(0), call.ArgAt<Guid>(1), call.ArgAt<long>(2))));
        return (publisher, calls);
    }
}
