using Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

// Work item: BUG-009 (FEAT-010), TD-007 (FEAT-010), FEAT-013
/// <summary>
/// Contains unit tests for the <see cref="UpdateProductValidator"/> class.
/// Tests cover the id, description limits and the unit price range and precision.
/// </summary>
public class UpdateProductValidatorTests
{
    private readonly UpdateProductValidator _validator = new();

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
        var command = ValidCommand();
        command.UnitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = _validator.Validate(command);

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
        var command = ValidCommand();
        command.UnitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = _validator.Validate(command);

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
        var command = ValidCommand();
        command.Description = new string('a', length);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("Description");
    }

    /// <summary>
    /// Tests that an empty product id is rejected.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When validated Then is invalid")]
    public void Given_EmptyId_When_Validated_Then_IsInvalid()
    {
        // Arrange
        var command = ValidCommand();
        command.Id = Guid.Empty;

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("Id");
    }

    // Work item: FEAT-013
    /// <summary>
    /// Tests that a code of up to 50 characters passes.
    /// </summary>
    [Theory(DisplayName = "Given a code within the length limit When validated Then is valid")]
    [InlineData(1)]
    [InlineData(50)]
    public void Given_CodeWithinLengthLimit_When_Validated_Then_IsValid(int length)
    {
        // Arrange
        var command = ValidCommand();
        command.Code = new string('A', length);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // Work item: FEAT-013
    /// <summary>
    /// Tests that an empty, blank, or longer than 50 characters code is rejected.
    /// </summary>
    [Theory(DisplayName = "Given an invalid code When validated Then is invalid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJX")]
    public void Given_InvalidCode_When_Validated_Then_IsInvalid(string code)
    {
        // Arrange
        var command = ValidCommand();
        command.Code = code;

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("Code");
    }

    // Work item: FEAT-013
    private static UpdateProductCommand ValidCommand() => new()
    {
        Id = Guid.NewGuid(),
        Code = "BEER-350",
        Description = "Beer 350ml",
        UnitPrice = 10m
    };
}
