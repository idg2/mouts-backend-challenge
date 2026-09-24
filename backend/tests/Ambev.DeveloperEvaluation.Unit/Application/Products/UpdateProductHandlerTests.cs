using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: TASK-020 (FEAT-010), FEAT-013
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

    // Work item: TASK-020 (FEAT-010), FEAT-013
    /// <summary>
    /// Tests that the new code, description, and unit price are applied to the stored product and saved.
    /// </summary>
    [Fact(DisplayName = "Given new product data When updating product Then saves the changes")]
    public async Task Given_ValidCommand_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Code = "BEER-350", Description = "Beer 350ml", UnitPrice = 10m };
        var command = new UpdateProductCommand { Id = product.Id, Code = " beer-473 ", Description = "Beer 473ml", UnitPrice = 12m };
        var expected = new UpdateProductResult { Id = product.Id, Description = command.Description, UnitPrice = command.UnitPrice };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _productRepository.UpdateAsync(product, Arg.Any<CancellationToken>()).Returns(product);
        _mapper.Map<UpdateProductResult>(product).Returns(expected);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        product.Code.Should().Be("BEER-473");
        product.Description.Should().Be("Beer 473ml");
        product.UnitPrice.Should().Be(12m);
        await _productRepository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    // Work item: FEAT-013
    /// <summary>
    /// Tests that a code used by another product, even in another case, is rejected and nothing is saved.
    /// </summary>
    [Fact(DisplayName = "Given a code used by another product When updating product Then throws duplicate entry exception")]
    public async Task Given_CodeUsedByAnotherProduct_When_Handled_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Code = "BEER-350", Description = "Beer 350ml", UnitPrice = 10m };
        var command = new UpdateProductCommand { Id = product.Id, Code = "beer-473", Description = "Beer 350ml", UnitPrice = 10m };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _productRepository.GetByCodeAsync("BEER-473", Arg.Any<CancellationToken>())
            .Returns(new Product { Id = Guid.NewGuid(), Code = "BEER-473" });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateEntryException>().WithMessage("Product with code BEER-473 already exists");
        await _productRepository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    // Work item: FEAT-013
    /// <summary>
    /// Tests that keeping the product's own code is not treated as a duplicate.
    /// </summary>
    [Fact(DisplayName = "Given the product's own code When updating product Then saves the changes")]
    public async Task Given_OwnCode_When_Handled_Then_SavesChanges()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Code = "BEER-350", Description = "Beer 350ml", UnitPrice = 10m };
        var command = new UpdateProductCommand { Id = product.Id, Code = "BEER-350", Description = "Beer 350ml can", UnitPrice = 10m };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _productRepository.GetByCodeAsync("BEER-350", Arg.Any<CancellationToken>()).Returns(product);
        _productRepository.UpdateAsync(product, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _productRepository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }
}
