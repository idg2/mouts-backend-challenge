using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
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

    // Work item: TASK-020 (FEAT-010), FEAT-013
    /// <summary>
    /// Tests that a valid command creates the product and returns its result.
    /// </summary>
    [Fact(DisplayName = "Given valid product data When creating product Then returns the created product")]
    public async Task Given_ValidCommand_When_Handled_Then_CreatesProductAndReturnsResult()
    {
        // Arrange
        var command = new CreateProductCommand { Code = "BEER-350", Description = "Beer 350ml", UnitPrice = 10m };
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

    // Work item: FEAT-013
    /// <summary>
    /// Tests that the code is trimmed and stored in upper case.
    /// </summary>
    [Fact(DisplayName = "Given a lower-case code with spaces When creating product Then stores it trimmed in upper case")]
    public async Task Given_LowerCaseCodeWithSpaces_When_Handled_Then_StoresNormalizedCode()
    {
        // Arrange
        var command = new CreateProductCommand { Code = "  beer-350 ", Description = "Beer 350ml", UnitPrice = 10m };
        var product = new Product { Code = command.Code, Description = command.Description, UnitPrice = command.UnitPrice };
        _mapper.Map<Product>(command).Returns(product);
        _productRepository.CreateAsync(product, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _productRepository.Received(1).CreateAsync(
            Arg.Is<Product>(p => p.Code == "BEER-350"), Arg.Any<CancellationToken>());
    }

    // Work item: FEAT-013
    /// <summary>
    /// Tests that a code already used by another product, even in another case, is rejected and nothing is created.
    /// </summary>
    [Fact(DisplayName = "Given a code already in use When creating product Then throws duplicate entry exception")]
    public async Task Given_CodeInUse_When_Handled_Then_ThrowsDuplicateEntryException()
    {
        // Arrange
        var command = new CreateProductCommand { Code = "beer-350", Description = "Beer 350ml", UnitPrice = 10m };
        _productRepository.GetByCodeAsync("BEER-350", Arg.Any<CancellationToken>())
            .Returns(new Product { Id = Guid.NewGuid(), Code = "BEER-350" });

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateEntryException>().WithMessage("Product with code BEER-350 already exists");
        await _productRepository.DidNotReceive().CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }
}
