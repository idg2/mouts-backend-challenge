using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="ListProductsHandler"/> class.
/// </summary>
public class ListProductsHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ListProductsHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public ListProductsHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new ListProductsHandler(_productRepository, _mapper);
    }

    /// <summary>
    /// Tests that the requested page and the total count are returned.
    /// </summary>
    [Fact(DisplayName = "Given a page request When listing products Then returns the page and the total count")]
    public async Task Given_PageRequest_When_Handled_Then_ReturnsPageAndTotalCount()
    {
        // Arrange
        var command = new ListProductsCommand { Page = 2, Size = 5 };
        IReadOnlyList<Product> products = new List<Product> { new() { Id = Guid.NewGuid(), Description = "Beer 350ml", UnitPrice = 10m } };
        var items = new List<ListProductsItem>
        {
            new() { Id = products[0].Id, Description = products[0].Description, UnitPrice = products[0].UnitPrice }
        };
        _productRepository.ListAsync(2, 5, Arg.Any<CancellationToken>()).Returns((products, 12));
        _mapper.Map<List<ListProductsItem>>(products).Returns(items);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Items.Should().BeSameAs(items);
        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(2);
        result.Size.Should().Be(5);
    }
}
