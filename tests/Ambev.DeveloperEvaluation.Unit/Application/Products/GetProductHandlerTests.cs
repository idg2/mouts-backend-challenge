using Ambev.DeveloperEvaluation.Application.Products.GetProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="GetProductHandler"/> class.
/// </summary>
public class GetProductHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly GetProductHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public GetProductHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetProductHandler(_productRepository, _mapper);
    }

    /// <summary>
    /// Tests that an existing product is returned.
    /// </summary>
    [Fact(DisplayName = "Given an existing product id When getting product Then returns the product")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsProduct()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Description = "Beer 350ml", UnitPrice = 10m };
        var expected = new GetProductResult { Id = product.Id, Description = product.Description, UnitPrice = product.UnitPrice };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _mapper.Map<GetProductResult>(product).Returns(expected);

        // Act
        var result = await _handler.Handle(new GetProductCommand(product.Id), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }
}
