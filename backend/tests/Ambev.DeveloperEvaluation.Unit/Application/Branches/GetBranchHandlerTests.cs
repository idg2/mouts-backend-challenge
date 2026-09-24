using Ambev.DeveloperEvaluation.Application.Branches.GetBranch;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Branches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="GetBranchHandler"/> class.
/// </summary>
public class GetBranchHandlerTests
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;
    private readonly GetBranchHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public GetBranchHandlerTests()
    {
        _branchRepository = Substitute.For<IBranchRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetBranchHandler(_branchRepository, _mapper);
    }

    /// <summary>
    /// Tests that an existing branch is returned.
    /// </summary>
    [Fact(DisplayName = "Given an existing branch id When getting branch Then returns the branch")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsBranch()
    {
        // Arrange
        var branch = new Branch { Id = Guid.NewGuid(), Name = "Downtown" };
        var expected = new GetBranchResult { Id = branch.Id, Name = branch.Name };
        _branchRepository.GetByIdAsync(branch.Id, Arg.Any<CancellationToken>()).Returns(branch);
        _mapper.Map<GetBranchResult>(branch).Returns(expected);

        // Act
        var result = await _handler.Handle(new GetBranchCommand(branch.Id), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }
}
