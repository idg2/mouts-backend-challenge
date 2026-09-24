using Ambev.DeveloperEvaluation.WebApi.Features.Products.CreateProduct;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Products;

// Work item: BUG-009 (FEAT-010), TD-007 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="CreateProductRequestValidator"/> class.
/// Tests cover description limits and the unit price range and precision.
/// </summary>
public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    /// <summary>
    /// Tests that unit prices that fit numeric(18,2), including trailing zeros, pass.
    /// </summary>
    [Theory(DisplayName = "Given a unit price within the stored precision When validated Then is valid")]
    [InlineData("0.01")]
    [InlineData("10.10")]
    [InlineData("9999999999999999.99")]
    public void Given_UnitPriceWithinPrecision_When_Validated_Then_IsValid(string unitPrice)
    {
        // Arrange
        var request = ValidRequest();
        request.UnitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that zero, too many decimals, and too many digits are rejected; 0.001 used to reach the CHECK constraint.
    /// </summary>
    [Theory(DisplayName = "Given an invalid unit price When validated Then is invalid")]
    [InlineData("0")]
    [InlineData("0.001")]
    [InlineData("10.123")]
    [InlineData("12345678901234567")]
    public void Given_InvalidUnitPrice_When_Validated_Then_IsInvalid(string unitPrice)
    {
        // Arrange
        var request = ValidRequest();
        request.UnitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("UnitPrice");
    }

    /// <summary>
    /// Tests that an empty description and one longer than 200 characters are rejected.
    /// </summary>
    [Theory(DisplayName = "Given an invalid description When validated Then is invalid")]
    [InlineData(0)]
    [InlineData(201)]
    public void Given_InvalidDescription_When_Validated_Then_IsInvalid(int length)
    {
        // Arrange
        var request = ValidRequest();
        request.Description = new string('a', length);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("Description");
    }

    private static CreateProductRequest ValidRequest() => new()
    {
        Description = "Beer 350ml",
        UnitPrice = 10m
    };
}
