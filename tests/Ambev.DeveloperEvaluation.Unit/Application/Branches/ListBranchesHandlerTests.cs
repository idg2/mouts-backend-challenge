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

    // Work item: TASK-025 (FEAT-011)
    /// <summary>
    /// Tests that the page, size, filters, and order reach the repository and the page and total count are returned.
    /// </summary>
    [Fact(DisplayName = "Given a page request with filters and order When listing branches Then passes them and returns the page")]
    public async Task Given_PageRequestWithFiltersAndOrder_When_Handled_Then_PassesThemAndReturnsPage()
    {
        // Arrange
        var filters = new List<FieldFilter> { new("Name", FilterOperator.Like, "acme%") };
        var order = new List<SortField> { new("Name", true) };
        var command = new ListBranchesCommand { Page = 2, Size = 5, Filters = filters, Order = order };
        IReadOnlyList<Branch> branches = new List<Branch> { new() { Id = Guid.NewGuid(), Name = "Downtown" } };
        var items = new List<ListBranchesItem> { new() { Id = branches[0].Id, Name = branches[0].Name } };
        _branchRepository.ListAsync(
                Arg.Is<ListQuery>(query => query.Page == 2 && query.Size == 5 && query.Filters == filters && query.Order == order),
                Arg.Any<CancellationToken>())
            .Returns((branches, 12));
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
