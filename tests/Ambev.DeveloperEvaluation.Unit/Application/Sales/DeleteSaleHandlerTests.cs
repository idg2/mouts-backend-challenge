using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Events.Sales;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="DeleteSaleHandler"/> class.
/// </summary>
public class DeleteSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly DeleteSaleHandler _handler;

    // Work item: TASK-029 (FEAT-004)
    private readonly IOutbox _outbox;

    // Work item: TASK-029 (FEAT-004)
    private readonly List<IIntegrationEvent> _enqueued = [];

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public DeleteSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _outbox = Substitute.For<IOutbox>();
        _outbox.When(outbox => outbox.EnqueueAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => _enqueued.Add(call.Arg<IIntegrationEvent>()));
        _handler = new DeleteSaleHandler(_saleRepository, _outbox);
    }

    /// <summary>
    /// Tests that deleting an existing sale reports success.
    /// </summary>
    [Fact(DisplayName = "Given an existing sale id When deleting sale Then returns success")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _saleRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _saleRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that deleting an existing sale enqueues SaleDeleted with its id.
    /// </summary>
    [Fact(DisplayName = "Given an existing sale id When deleting sale Then enqueues SaleDeleted")]
    public async Task Given_ExistingId_When_Handled_Then_EnqueuesSaleDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        _saleRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        // Assert
        _enqueued.Should().Equal(new SaleDeleted(id));
    }

    // Work item: TASK-029 (FEAT-004)
    /// <summary>
    /// Tests that deleting an unknown sale throws before enqueueing anything.
    /// </summary>
    [Fact(DisplayName = "Given an unknown sale id When deleting sale Then enqueues nothing")]
    public async Task Given_UnknownId_When_Handled_Then_EnqueuesNothing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _saleRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var act = () => _handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _enqueued.Should().BeEmpty();
    }
}
