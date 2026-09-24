using Ambev.DeveloperEvaluation.Application.Branches.ListBranches;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Branches;

// Work item: TASK-019 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="ListBranchesHandler"/> class.
/// </summary>
public class ListBranchesHandlerTests
{
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;
    private readonly ListBranchesHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public ListBranchesHandlerTests()
    {
        _branchRepository = Substitute.For<IBranchRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new ListBranchesHandler(_branchRepository, _mapper);
    }

    /// <summary>
    /// Tests that the requested page and the total count are returned.
    /// </summary>
    [Fact(DisplayName = "Given a page request When listing branches Then returns the page and the total count")]
    public async Task Given_PageRequest_When_Handled_Then_ReturnsPageAndTotalCount()
    {
        // Arrange
        var command = new ListBranchesCommand { Page = 2, Size = 5 };
        IReadOnlyList<Branch> branches = new List<Branch> { new() { Id = Guid.NewGuid(), Name = "Downtown" } };
        var items = new List<ListBranchesItem> { new() { Id = branches[0].Id, Name = branches[0].Name } };
        _branchRepository.ListAsync(2, 5, Arg.Any<CancellationToken>()).Returns((branches, 12));
        _mapper.Map<List<ListBranchesItem>>(branches).Returns(items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Items.Should().BeSameAs(items);
        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(2);
        result.Size.Should().Be(5);
    }
}
