using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="CreateProductHandler"/> class.
/// </summary>
public class CreateProductHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly CreateProductHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public CreateProductHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new CreateProductHandler(_productRepository, _mapper);
    }

    /// <summary>
    /// Tests that a valid command creates the product and returns its result.
    /// </summary>
    [Fact(DisplayName = "Given valid product data When creating product Then returns the created product")]
    public async Task Given_ValidCommand_When_Handled_Then_CreatesProductAndReturnsResult()
    {
        // Arrange
        var command = new CreateProductCommand { Description = "Beer 350ml", UnitPrice = 10m };
        var product = new Product { Id = Guid.NewGuid(), Description = command.Description, UnitPrice = command.UnitPrice };
        var expected = new CreateProductResult { Id = product.Id, Description = product.Description, UnitPrice = product.UnitPrice };
        _mapper.Map<Product>(command).Returns(product);
        _productRepository.CreateAsync(product, Arg.Any<CancellationToken>()).Returns(product);
        _mapper.Map<CreateProductResult>(product).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        await _productRepository.Received(1).CreateAsync(product, Arg.Any<CancellationToken>());
    }
}
