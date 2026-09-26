using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

// Work item: TASK-014 (FEAT-010), FEAT-013
/// <summary>
/// Contains unit tests for the Product entity class.
/// </summary>
public class ProductTests
{
    // Work item: TASK-014 (FEAT-010), FEAT-013
    /// <summary>
    /// Tests that validation passes when the product data is valid.
    /// </summary>
    [Fact(DisplayName = "Validation should pass for valid product data")]
    public void Given_ValidProduct_When_Validated_Then_ShouldReturnValid()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Code = "BEER-350", Description = "Beer 350ml", UnitPrice = 10m };

        // Act
        var result = product.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // Work item: BUG-009 (FEAT-010), FEAT-013
    /// <summary>
    /// Tests that unit prices that do not fit numeric(18,2) or are not positive are rejected.
    /// </summary>
    [Theory(DisplayName = "Validation should fail for a unit price beyond the stored precision")]
    [InlineData("0")]
    [InlineData("0.001")]
    [InlineData("10.123")]
    [InlineData("12345678901234567")]
    public void Given_InvalidUnitPrice_When_Validated_Then_ShouldReturnInvalid(string unitPrice)
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "BEER-350",
            Description = "Beer 350ml",
            UnitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture)
        };

        // Act
        var result = product.Validate();

        // Assert
        Assert.False(result.IsValid);
    }

    // Work item: FEAT-013
    /// <summary>
    /// Tests that an empty or longer than 50 characters code is rejected.
    /// </summary>
    [Theory(DisplayName = "Validation should fail for an empty or too long code")]
    [InlineData("")]
    [InlineData("ABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJX")]
    public void Given_InvalidCode_When_Validated_Then_ShouldReturnInvalid(string code)
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Code = code, Description = "Beer 350ml", UnitPrice = 10m };

        // Act
        var result = product.Validate();

        // Assert
        Assert.False(result.IsValid);
    }
}
