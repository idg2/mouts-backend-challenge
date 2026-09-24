using Ambev.DeveloperEvaluation.Application.Branches.DeleteBranch;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Branches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="DeleteBranchHandler"/> class.
/// </summary>
public class DeleteBranchHandlerTests
{
    private readonly IBranchRepository _branchRepository;
    private readonly DeleteBranchHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public DeleteBranchHandlerTests()
    {
        _branchRepository = Substitute.For<IBranchRepository>();
        _handler = new DeleteBranchHandler(_branchRepository);
    }

    /// <summary>
    /// Tests that deleting an existing branch reports success.
    /// </summary>
    [Fact(DisplayName = "Given an existing branch id When deleting branch Then returns success")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _branchRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new DeleteBranchCommand(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _branchRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
