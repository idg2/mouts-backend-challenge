using Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="DeleteProductHandler"/> class.
/// </summary>
public class DeleteProductHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly DeleteProductHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public DeleteProductHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _handler = new DeleteProductHandler(_productRepository);
    }

    /// <summary>
    /// Tests that deleting an existing product reports success.
    /// </summary>
    [Fact(DisplayName = "Given an existing product id When deleting product Then returns success")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _productRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new DeleteProductCommand(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _productRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
