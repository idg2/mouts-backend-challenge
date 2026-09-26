using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using FluentAssertions;
using NSubstitute;
using Rebus.Pipeline;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging;

// Work item: TASK-076 (FEAT-003)
/// <summary>
/// Contains unit tests for the <see cref="SaleProjectionHandler"/> class.
/// </summary>
public class SaleProjectionHandlerTests
{
    private readonly ISaleReadStore _store = Substitute.For<ISaleReadStore>();
    private readonly IMessageContext _messageContext = Substitute.For<IMessageContext>();

    /// <summary>
    /// Tests that a snapshot event is upserted with the sequence read from the header.
    /// </summary>
    [Theory(DisplayName = "Given a snapshot event with a sequence header When handled Then upserts with that sequence")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Given_SnapshotEvent_When_Handled_Then_UpsertsWithSequence(bool created)
    {
        // Arrange
        var snapshot = Snapshot();
        Headers(("outbox-sequence", "12"));
        var handler = new SaleProjectionHandler(_store, _messageContext);

        // Act
        if (created)
            await handler.Handle(new SaleCreated(snapshot));
        else
            await handler.Handle(new SaleModified(snapshot));

        // Assert
        await _store.Received(1).UpsertAsync(snapshot, 12, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that SaleDeleted becomes a tombstone with the sequence of the header.
    /// </summary>
    [Fact(DisplayName = "Given SaleDeleted with a sequence header When handled Then marks the sale deleted with that sequence")]
    public async Task Given_SaleDeleted_When_Handled_Then_MarksDeletedWithSequence()
    {
        // Arrange
        var saleId = Guid.NewGuid();
        Headers(("outbox-sequence", "13"));

        // Act
        await new SaleProjectionHandler(_store, _messageContext).Handle(new SaleDeleted(saleId));

        // Assert
        await _store.Received(1).MarkDeletedAsync(saleId, 13, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that the cancellation events touch nothing: the preceding SaleModified already carried the state.
    /// </summary>
    [Fact(DisplayName = "Given SaleCancelled and ItemCancelled When handled Then the store is not touched")]
    public async Task Given_CancellationEvents_When_Handled_Then_StoreNotTouched()
    {
        // Arrange
        Headers(("outbox-sequence", "14"));
        var handler = new SaleProjectionHandler(_store, _messageContext);

        // Act
        await handler.Handle(new SaleCancelled(Guid.NewGuid()));
        await handler.Handle(new ItemCancelled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        _store.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a message without a usable sequence header is refused with a message naming the header
    /// (Review Focus 4: a message queued before this version).
    /// </summary>
    [Theory(DisplayName = "Given a missing or invalid sequence header When handled Then throws naming the header")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("twelve")]
    [InlineData("-1")]
    public async Task Given_MissingOrInvalidSequenceHeader_When_Handled_Then_ThrowsNamingHeader(string? value)
    {
        // Arrange
        if (value is null)
            Headers();
        else
            Headers(("outbox-sequence", value));

        // Act
        var act = () => new SaleProjectionHandler(_store, _messageContext).Handle(new SaleDeleted(Guid.NewGuid()));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*outbox-sequence*");
        _store.ReceivedCalls().Should().BeEmpty();
    }

    private void Headers(params (string Name, string Value)[] headers) =>
        _messageContext.Headers.Returns(headers.ToDictionary(header => header.Name, header => header.Value));

    private static SaleSnapshot Snapshot() => new(
        Guid.NewGuid(), 1, DateTime.UtcNow, Guid.NewGuid(), "Acme", Guid.NewGuid(), "Downtown", 10m, false, []);
}
