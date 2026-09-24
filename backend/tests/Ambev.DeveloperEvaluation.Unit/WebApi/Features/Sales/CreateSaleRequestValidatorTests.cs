using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.Sales;

// Work item: BUG-007 (FEAT-010), BUG-009 (FEAT-010), TD-007 (FEAT-010)
/// <summary>
/// Contains unit tests for the <see cref="CreateSaleRequestValidator"/> class.
/// Tests cover null items, empty items and ids, and decimal precision limits.
/// </summary>
public class CreateSaleRequestValidatorTests
{
    private readonly CreateSaleRequestValidator _validator = new();

    /// <summary>
    /// Tests that a well-formed request passes, including amounts with trailing zeros.
    /// </summary>
    [Fact(DisplayName = "Given a valid request When validated Then is valid")]
    public void Given_ValidRequest_When_Validated_Then_IsValid()
    {
        // Arrange
        var request = ValidRequest();
        request.Items[0].TotalAmount = 10.10m;

        // Act
        var result = _validator.Validate(request);

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
        var request = ValidRequest();
        request.Items = [null!];

        // Act
        var result = _validator.Validate(request);

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
        var request = ValidRequest();
        request.Items = [];

        // Act
        var result = _validator.Validate(request);

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
        var request = ValidRequest();
        request.CustomerId = Guid.Empty;
        request.BranchId = Guid.Empty;
        request.Items[0].ProductId = Guid.Empty;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should()
            .Contain(["CustomerId", "BranchId", "Items[0].ProductId"]);
    }

    /// <summary>
    /// Tests that amounts that do not fit numeric(18,2) and percentages that do not fit numeric(5,2) are rejected.
    /// </summary>
    [Theory(DisplayName = "Given an amount beyond the stored precision When validated Then is invalid")]
    [InlineData("TotalAmount", "10.123")]
    [InlineData("TotalAmount", "12345678901234567")]
    [InlineData("Items[0].DiscountAmount", "0.001")]
    [InlineData("Items[0].TotalAmount", "10.125")]
    [InlineData("Items[0].DiscountPercentage", "12.345")]
    public void Given_AmountBeyondPrecision_When_Validated_Then_IsInvalid(string property, string value)
    {
        // Arrange
        var request = ValidRequest();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        switch (property)
        {
            case "TotalAmount": request.TotalAmount = amount; break;
            case "Items[0].DiscountAmount": request.Items[0].DiscountAmount = amount; break;
            case "Items[0].TotalAmount": request.Items[0].TotalAmount = amount; break;
            case "Items[0].DiscountPercentage": request.Items[0].DiscountPercentage = amount; break;
        }

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain(property);
    }

    private static CreateSaleRequest ValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        BranchId = Guid.NewGuid(),
        TotalAmount = 20m,
        Items =
        [
            new CreateSaleItemRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity = 2,
                DiscountPercentage = 10m,
                DiscountAmount = 2m,
                TotalAmount = 18m
            }
        ]
    };
}
