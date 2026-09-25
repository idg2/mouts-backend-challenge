using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-028 (FEAT-004)
/// <summary>
/// Contains integration tests for the <see cref="OutboxWriter"/> class.
/// </summary>
public class OutboxWriterTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public OutboxWriterTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that an enqueue outside a transaction is refused, so an event can never be stored apart from its write.
    /// </summary>
    [Fact(DisplayName = "Given no open transaction When enqueueing Then throws and stores nothing")]
    public async Task Given_NoTransaction_When_Enqueueing_Then_ThrowsAndStoresNothing()
    {
        // Arrange
        var before = await CountAsync();
        await using var context = _fixture.CreateContext();
        var writer = new OutboxWriter(context, TimeProvider.System);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.EnqueueAsync(new SaleDeleted(Guid.NewGuid())));

        // Assert
        Assert.Contains("transaction", exception.Message);
        Assert.Equal(before, await CountAsync());
    }

    /// <summary>
    /// Tests that an event enqueued inside a rolled back transaction is not stored.
    /// </summary>
    [Fact(DisplayName = "Given an enqueue inside a transaction When rolling back Then no row is stored")]
    public async Task Given_EnqueueInsideTransaction_When_RollingBack_Then_NoRowIsStored()
    {
        // Arrange
        var before = await CountAsync();
        await using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        await unitOfWork.BeginTransactionAsync();
        await new OutboxWriter(context, TimeProvider.System).EnqueueAsync(new SaleDeleted(Guid.NewGuid()));

        // Act
        await unitOfWork.RollbackTransactionAsync();

        // Assert
        Assert.Equal(before, await CountAsync());
    }

    /// <summary>
    /// Tests that committed events are stored pending, with increasing sequence in enqueue order, and that the
    /// payload deserializes back to the same values, including decimals and a UTC date with milliseconds.
    /// </summary>
    [Fact(DisplayName = "Given an enqueue inside a transaction When committing Then rows are stored in order and the payload round-trips")]
    public async Task Given_EnqueueInsideTransaction_When_Committing_Then_RowsAreStoredInOrderAndPayloadRoundTrips()
    {
        // Arrange
        var snapshot = new SaleSnapshot(
            Guid.NewGuid(), 42, new DateTime(2026, 9, 24, 13, 45, 30, 123, DateTimeKind.Utc),
            Guid.NewGuid(), "Acme Market", Guid.NewGuid(), "Downtown", 21.98m, false,
            [new SaleSnapshotItem(Guid.NewGuid(), 1, Guid.NewGuid(), "Beer 350ml", 10.99m, 2, 0m, 0m, 21.98m, false)]);
        long lastSequence;
        await using (var reader = _fixture.CreateContext())
            lastSequence = await reader.OutboxMessages.MaxAsync(message => (long?)message.Sequence) ?? 0;

        await using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        await unitOfWork.BeginTransactionAsync();
        var writer = new OutboxWriter(context, TimeProvider.System);
        await writer.EnqueueAsync(new SaleCreated(snapshot));
        await writer.EnqueueAsync(new SaleCancelled(snapshot.SaleId));

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        await using var check = _fixture.CreateContext();
        var rows = await check.OutboxMessages
            .Where(message => message.Sequence > lastSequence)
            .OrderBy(message => message.Sequence)
            .ToListAsync();
        Assert.Equal(new[] { "SaleCreated", "SaleCancelled" }, rows.Select(row => row.Type));
        Assert.True(rows[0].Sequence < rows[1].Sequence);
        Assert.All(rows, row => Assert.Null(row.ProcessedAt));
        var created = Assert.IsType<SaleCreated>(
            JsonSerializer.Deserialize(rows[0].Payload, IntegrationEventTypes.Find(rows[0].Type)!));
        Assert.Equal(snapshot with { Items = created.Sale.Items }, created.Sale);
        Assert.Equal(snapshot.Items, created.Sale.Items);
        Assert.Equal(DateTimeKind.Utc, created.Sale.SaleDate.Kind);
        var cancelled = Assert.IsType<SaleCancelled>(
            JsonSerializer.Deserialize(rows[1].Payload, IntegrationEventTypes.Find(rows[1].Type)!));
        Assert.Equal(snapshot.SaleId, cancelled.SaleId);
    }

    private async Task<int> CountAsync()
    {
        await using var context = _fixture.CreateContext();
        return await context.OutboxMessages.CountAsync();
    }
}
