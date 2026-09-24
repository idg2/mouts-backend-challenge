using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="UpdateProductHandler"/> class.
/// </summary>
public class UpdateProductHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly UpdateProductHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public UpdateProductHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new UpdateProductHandler(_productRepository, _mapper);
    }

    /// <summary>
    /// Tests that the new description and unit price are applied to the stored product and saved.
    /// </summary>
    [Fact(DisplayName = "Given new product data When updating product Then saves the changes")]
    public async Task Given_ValidCommand_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Description = "Beer 350ml", UnitPrice = 10m };
        var command = new UpdateProductCommand { Id = product.Id, Description = "Beer 473ml", UnitPrice = 12m };
        var expected = new UpdateProductResult { Id = product.Id, Description = command.Description, UnitPrice = command.UnitPrice };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _productRepository.UpdateAsync(product, Arg.Any<CancellationToken>()).Returns(product);
        _mapper.Map<UpdateProductResult>(product).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        product.Description.Should().Be("Beer 473ml");
        product.UnitPrice.Should().Be(12m);
        await _productRepository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }
}
