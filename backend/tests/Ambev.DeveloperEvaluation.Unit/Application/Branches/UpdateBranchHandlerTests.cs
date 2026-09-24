using Ambev.DeveloperEvaluation.Application.Branches.UpdateBranch;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Branches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="UpdateBranchHandler"/> class.
/// </summary>
public class UpdateBranchHandlerTests
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;
    private readonly UpdateBranchHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public UpdateBranchHandlerTests()
    {
        _branchRepository = Substitute.For<IBranchRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new UpdateBranchHandler(_branchRepository, _mapper);
    }

    /// <summary>
    /// Tests that the new name is applied to the stored branch and saved.
    /// </summary>
    [Fact(DisplayName = "Given new branch data When updating branch Then saves the changes")]
    public async Task Given_ValidCommand_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var branch = new Branch { Id = Guid.NewGuid(), Name = "Downtown" };
        var command = new UpdateBranchCommand { Id = branch.Id, Name = "Uptown" };
        var expected = new UpdateBranchResult { Id = branch.Id, Name = command.Name };
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _branchRepository.UpdateAsync(branch, Arg.Any<CancellationToken>()).Returns(branch);
        _mapper.Map<UpdateBranchResult>(branch).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        branch.Name.Should().Be("Uptown");
        await _branchRepository.Received(1).UpdateAsync(branch, Arg.Any<CancellationToken>());
    }
}
