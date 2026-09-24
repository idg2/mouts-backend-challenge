using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="ListSalesHandler"/> class.
/// </summary>
public class ListSalesHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ListSalesHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public ListSalesHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new ListSalesHandler(_saleRepository, _mapper);
    }

    // Work item: TASK-025 (FEAT-011)
    /// <summary>
    /// Tests that the page, size, filters, and order reach the repository and the page and total count are returned.
    /// </summary>
    [Fact(DisplayName = "Given a page request with filters and order When listing sales Then passes them and returns the page")]
    public async Task Given_PageRequestWithFiltersAndOrder_When_Handled_Then_PassesThemAndReturnsPage()
    {
        // Arrange
        var filters = new List<FieldFilter> { new("IsCancelled", FilterOperator.Equal, false) };
        var order = new List<SortField> { new("SaleDate", true) };
        var command = new ListSalesCommand { Page = 3, Size = 20, Filters = filters, Order = order };
        IReadOnlyList<Sale> sales = new List<Sale> { new() { Id = Guid.NewGuid(), SaleNumber = 41 } };
        var items = new List<ListSalesItem> { new() { Id = sales[0].Id, SaleNumber = 41 } };
        _saleRepository.ListAsync(
                Arg.Is<ListQuery>(query => query.Page == 3 && query.Size == 20 && query.Filters == filters && query.Order == order),
                Arg.Any<CancellationToken>())
            .Returns((sales, 41));
        _mapper.Map<List<ListSalesItem>>(sales).Returns(items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Items.Should().BeSameAs(items);
        result.TotalCount.Should().Be(41);
        result.Page.Should().Be(3);
        result.Size.Should().Be(20);
    }
}
