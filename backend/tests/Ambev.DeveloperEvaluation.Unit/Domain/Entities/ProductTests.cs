using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Contains unit tests for the Product entity class.
/// </summary>
public class ProductTests
{
    /// <summary>
    /// Tests that validation passes when the product data is valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid product data")]
    public void Given_ValidProduct_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Description = "Beer 350ml", UnitPrice = 10m };

        // Act
        var result = product.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
