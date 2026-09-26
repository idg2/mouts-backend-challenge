using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

// Work item: BUG-007 (FEAT-010), BUG-009 (FEAT-010), TD-007 (FEAT-010), TASK-064 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="CreateSaleValidator"/> class.
/// Tests cover null items, empty items and ids, and decimal precision limits.
/// </summary>
public class CreateSaleValidatorTests
{
    private readonly CreateSaleValidator _validator = new();

    // Work item: TASK-064 (FEAT-001), TASK-066 (FEAT-001)
    /// <summary>
    /// Tests that a well-formed command passes, including a discount percentage with two decimals.
    /// </summary>
    [Fact(DisplayName = "Given a valid command When validated Then is valid")]
    public void Given_ValidCommand_When_Validated_Then_IsValid()
    {
        // Arrange
        var command = ValidCommand();
        command.Items[0].DiscountPercentage = 10.10m;

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a null item is rejected instead of passing through to the handler.
    /// </summary>
    [Fact(DisplayName = "Given a null item When validated Then is invalid")]
    public void Given_NullItem_When_Validated_Then_IsInvalid()
    {
        // Arrange
        var command = ValidCommand();
        command.Items = [null!];

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a sale without items is rejected.
    /// </summary>
    [Fact(DisplayName = "Given no items When validated Then is invalid")]
    public void Given_NoItems_When_Validated_Then_IsInvalid()
    {
        // Arrange
        var command = ValidCommand();
        command.Items = [];

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that empty customer, branch, and product ids are rejected.
    /// </summary>
    [Fact(DisplayName = "Given empty ids When validated Then reports each id")]
    public void Given_EmptyIds_When_Validated_Then_ReportsEachId()
    {
        // Arrange
        var command = ValidCommand();
        command.CustomerId = Guid.Empty;
        command.BranchId = Guid.Empty;
        command.Items[0].ProductId = Guid.Empty;

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should()
            .Contain(["CustomerId", "BranchId", "Items[0].ProductId"]);
    }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Tests that a requested discount percentage out of range or beyond numeric(5,2) is rejected.
    /// </summary>
    [Theory(DisplayName = "Given a discount percentage out of range or beyond the stored precision When validated Then is invalid")]
    [InlineData("12.345")]
    [InlineData("100.01")]
    [InlineData("-1")]
    public void Given_BadDiscountPercentage_When_Validated_Then_IsInvalid(string value)
    {
        // Arrange
        var command = ValidCommand();
        command.Items[0].DiscountPercentage = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("Items[0].DiscountPercentage");
    }

    // Work item: TASK-064 (FEAT-001)
    /// <summary>
    /// Tests that an item without a requested discount is valid: it receives the ceiling.
    /// </summary>
    [Fact(DisplayName = "Given no discount percentage When validated Then is valid")]
    public void Given_NoDiscountPercentage_When_Validated_Then_IsValid()
    {
        // Arrange
        var command = ValidCommand();
        command.Items[0].DiscountPercentage = null;

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // Work item: TASK-064 (FEAT-001)
    private static CreateSaleCommand ValidCommand() => new()
    {
        CustomerId = Guid.NewGuid(),
        BranchId = Guid.NewGuid(),
        Items = [new CreateSaleItemInput { ProductId = Guid.NewGuid(), Quantity = 2, DiscountPercentage = 10m }]
    };
}
