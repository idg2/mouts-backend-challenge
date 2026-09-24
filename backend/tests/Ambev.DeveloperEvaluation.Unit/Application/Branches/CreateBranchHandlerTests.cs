using Ambev.DeveloperEvaluation.Application.Branches.CreateBranch;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Branches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="CreateBranchHandler"/> class.
/// </summary>
public class CreateBranchHandlerTests
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;
    private readonly CreateBranchHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public CreateBranchHandlerTests()
    {
        _branchRepository = Substitute.For<IBranchRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new CreateBranchHandler(_branchRepository, _mapper);
    }

    /// <summary>
    /// Tests that a valid command creates the branch and returns its result.
    /// </summary>
    [Fact(DisplayName = "Given valid branch data When creating branch Then returns the created branch")]
    public async Task Given_ValidCommand_When_Handled_Then_CreatesBranchAndReturnsResult()
    {
        // Arrange
        var command = new CreateBranchCommand { Name = "Downtown" };
        var branch = new Branch { Id = Guid.NewGuid(), Name = command.Name };
        var expected = new CreateBranchResult { Id = branch.Id, Name = branch.Name };
        _mapper.Map<Branch>(command).Returns(branch);
        _branchRepository.CreateAsync(branch, Arg.Any<CancellationToken>()).Returns(branch);
        _mapper.Map<CreateBranchResult>(branch).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        await _branchRepository.Received(1).CreateAsync(branch, Arg.Any<CancellationToken>());
    }
}
