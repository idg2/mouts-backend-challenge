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

    /// <summary>
    /// Tests that the requested page and the total count are returned.
    /// </summary>
    [Fact(DisplayName = "Given a page request When listing sales Then returns the page and the total count")]
    public async Task Given_PageRequest_When_Handled_Then_ReturnsPageAndTotalCount()
    {
        // Arrange
        var command = new ListSalesCommand { Page = 3, Size = 20 };
        IReadOnlyList<Sale> sales = new List<Sale> { new() { Id = Guid.NewGuid(), SaleNumber = 41 } };
        var items = new List<ListSalesItem> { new() { Id = sales[0].Id, SaleNumber = 41 } };
        _saleRepository.ListAsync(3, 20, Arg.Any<CancellationToken>()).Returns((sales, 41));
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
