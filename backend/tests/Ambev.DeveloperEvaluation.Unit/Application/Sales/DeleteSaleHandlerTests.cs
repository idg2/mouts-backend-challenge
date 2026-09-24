using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="DeleteSaleHandler"/> class.
/// </summary>
public class DeleteSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository;
    private readonly DeleteSaleHandler _handler;

    /// <summary>
    /// Initializes the test dependencies.
    /// </summary>
    public DeleteSaleHandlerTests()
    {
        _saleRepository = Substitute.For<ISaleRepository>();
        _handler = new DeleteSaleHandler(_saleRepository);
    }

    /// <summary>
    /// Tests that deleting an existing sale reports success.
    /// </summary>
    [Fact(DisplayName = "Given an existing sale id When deleting sale Then returns success")]
    public async Task Given_ExistingId_When_Handled_Then_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _saleRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new DeleteSaleCommand(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _saleRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
