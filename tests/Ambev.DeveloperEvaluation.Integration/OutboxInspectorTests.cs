#if DEBUG
using Ambev.DeveloperEvaluation.ORM.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration;

// Work item: TASK-083 (FEAT-019)
/// <summary>
/// Contains integration tests for the <see cref="OutboxInspector"/> class. Each test empties the outbox first,
/// since the tests of the class share one throwaway database.
/// </summary>
public class OutboxInspectorTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes the tests with the shared throwaway database.
    /// </summary>
    public OutboxInspectorTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // Work item: TASK-083 (FEAT-019)
    [Fact(DisplayName = "Given an empty outbox When reading the head Then it is zero")]
    public async Task Given_EmptyOutbox_When_ReadingHead_Then_Zero()
    {
        // Arrange
        await EmptyAsync();
        await using var context = _fixture.CreateContext();

        // Act
        var head = await new OutboxInspector(context).HeadAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, head);
    }

    // Work item: TASK-083 (FEAT-019)
    [Fact(DisplayName = "Given rows When reading after a sequence Then later rows return in sequence order and the head is the last")]
    public async Task Given_Rows_When_ReadingAfterSequence_Then_LaterRowsInOrderAndHeadIsLast()
    {
        // Arrange
        await EmptyAsync();
        var rows = await SeedAsync("SaleCreated", "SaleModified", "SaleDeleted");
        await using var context = _fixture.CreateContext();
        var inspector = new OutboxInspector(context);

        // Act
        var later = await inspector.ReadAfterAsync(rows[0].Sequence, CancellationToken.None);
        var head = await inspector.HeadAsync(CancellationToken.None);

        // Assert
        Assert.Equal(new[] { rows[1].Id, rows[2].Id }, later.Select(row => row.Id));
        Assert.Equal(new[] { "SaleModified", "SaleDeleted" }, later.Select(row => row.Type));
        Assert.Equal(rows[2].Sequence, head);
    }

    // Work item: TASK-083 (FEAT-019)
    [Fact(DisplayName = "Given more rows than the cap When reading after zero Then only the oldest capped rows return")]
    public async Task Given_MoreRowsThanCap_When_ReadingAfterZero_Then_OldestCappedRowsReturn()
    {
        // Arrange
        await EmptyAsync();
        var rows = await SeedAsync(Enumerable.Repeat("SaleDeleted", OutboxInspector.MaxRows + 1).ToArray());
        await using var context = _fixture.CreateContext();

        // Act
        var read = await new OutboxInspector(context).ReadAfterAsync(0, CancellationToken.None);

        // Assert
        Assert.Equal(OutboxInspector.MaxRows, read.Count);
        Assert.Equal(rows[0].Id, read[0].Id);
    }

    private async Task EmptyAsync()
    {
        await using var context = _fixture.CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
    }

    private async Task<IReadOnlyList<OutboxMessage>> SeedAsync(params string[] types)
    {
        await using var context = _fixture.CreateContext();
        var rows = types.Select(type => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = "{\"SaleId\":\"" + Guid.NewGuid() + "\"}",
            OccurredAt = DateTime.UtcNow
        }).ToList();
        foreach (var row in rows)
        {
            context.OutboxMessages.Add(row);
            await context.SaveChangesAsync();
        }

        return rows;
    }
}
#endif
