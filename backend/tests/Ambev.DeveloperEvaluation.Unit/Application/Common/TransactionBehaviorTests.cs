using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Common;

// Work item: TD-006
/// <summary>
/// Contains unit tests for the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
/// </summary>
public class TransactionBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Tests that a transactional command runs its handler inside a transaction that is committed.
    /// </summary>
    [Fact(DisplayName = "Given a transactional command When the handler succeeds Then the transaction is committed")]
    public async Task Given_TransactionalCommand_When_HandlerSucceeds_Then_TransactionIsCommitted()
    {
        // Arrange
        var behavior = new TransactionBehavior<WriteRequest, string>(_unitOfWork);

        // Act
        var result = await behavior.Handle(new WriteRequest(), () =>
        {
            _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
            return Task.FromResult("done");
        }, CancellationToken.None);

        // Assert
        result.Should().Be("done");
        Received.InOrder(() =>
        {
            _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _unitOfWork.CommitTransactionAsync(Arg.Any<CancellationToken>());
        });
        await _unitOfWork.DidNotReceive().RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that a transactional command whose handler throws rolls the transaction back and rethrows.
    /// </summary>
    [Fact(DisplayName = "Given a transactional command When the handler throws Then the transaction is rolled back")]
    public async Task Given_TransactionalCommand_When_HandlerThrows_Then_TransactionIsRolledBack()
    {
        // Arrange
        var behavior = new TransactionBehavior<WriteRequest, string>(_unitOfWork);
        var failure = new InvalidOperationException("handler failed");

        // Act
        var act = () => behavior.Handle(new WriteRequest(), () => throw failure, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that a request not marked as transactional runs without a transaction.
    /// </summary>
    [Fact(DisplayName = "Given a non-transactional request When handling Then no transaction is opened")]
    public async Task Given_NonTransactionalRequest_When_Handling_Then_NoTransactionIsOpened()
    {
        // Arrange
        var behavior = new TransactionBehavior<ReadRequest, string>(_unitOfWork);

        // Act
        var result = await behavior.Handle(new ReadRequest(), () => Task.FromResult("read"), CancellationToken.None);

        // Assert
        result.Should().Be("read");
        _unitOfWork.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// A request marked as transactional.
    /// </summary>
    public record WriteRequest : IRequest<string>, ITransactionalCommand;

    /// <summary>
    /// A request not marked as transactional.
    /// </summary>
    public record ReadRequest : IRequest<string>;
}
