using Ambev.DeveloperEvaluation.Application.DiscountPolicies.Common;
using Ambev.DeveloperEvaluation.Application.DiscountPolicies.CreateDiscountPolicy;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.DiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="CreateDiscountPolicyValidator"/> class: the structural rules of spec section
/// 3.3 and UTC dates.
/// </summary>
public class CreateDiscountPolicyValidatorTests
{
    private static readonly DateTime Now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
    private readonly CreateDiscountPolicyValidator _validator = new();

    [Fact(DisplayName = "Given a valid command When validated Then is valid")]
    public void Given_ValidCommand_When_Validated_Then_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Given no tiers When validated Then is valid")]
    public void Given_NoTiers_When_Validated_Then_IsValid()
    {
        // Arrange
        var command = Valid();
        command.Tiers = [];

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Given an invalid field When validated Then reports that field")]
    [InlineData("ProductId")]
    [InlineData("BranchId")]
    [InlineData("ValidFrom")]
    [InlineData("ValidTo")]
    [InlineData("MaxQuantityPerProduct")]
    [InlineData("Tiers[0].MinQuantity")]
    [InlineData("Tiers[0].MaxQuantity")]
    [InlineData("Tiers[0].Percentage")]
    public void Given_InvalidField_When_Validated_Then_ReportsField(string field)
    {
        // Arrange
        var command = Valid();
        switch (field)
        {
            case "ProductId": command.ProductId = Guid.Empty; break;
            case "BranchId": command.BranchId = Guid.Empty; break;
            case "ValidFrom": command.ValidFrom = DateTime.SpecifyKind(Now.AddDays(1), DateTimeKind.Unspecified); break;
            case "ValidTo": command.ValidTo = command.ValidFrom; break;
            case "MaxQuantityPerProduct": command.MaxQuantityPerProduct = 0; break;
            case "Tiers[0].MinQuantity": command.Tiers[0].MinQuantity = 0; break;
            case "Tiers[0].MaxQuantity": command.Tiers[0].MaxQuantity = 3; break;
            case "Tiers[0].Percentage": command.Tiers[0].Percentage = 0m; break;
        }

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(error => error.PropertyName).Should().Contain(field);
    }

    [Theory(DisplayName = "Given a percentage out of range or with three decimals When validated Then reports the percentage")]
    [InlineData("100.01")]
    [InlineData("12.345")]
    public void Given_BadPercentage_When_Validated_Then_ReportsPercentage(string value)
    {
        // Arrange
        var command = Valid();
        command.Tiers[0].Percentage = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Select(error => error.PropertyName).Should().Contain("Tiers[0].Percentage");
    }

    // Work item: TASK-066 (FEAT-001)
    [Fact(DisplayName = "Given a ValidTo equal to ValidFrom When validated Then reports ValidToNotAfterValidFrom on ValidTo")]
    public void Given_ValidToEqualToValidFrom_When_Validated_Then_ValidToNotAfterValidFrom()
    {
        // Arrange
        var command = Valid();
        command.ValidTo = command.ValidFrom;

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().ContainSingle(error => error.PropertyName == "ValidTo")
            .Which.ErrorCode.Should().Be("ValidToNotAfterValidFrom");
    }

    [Fact(DisplayName = "Given a local ValidTo When validated Then reports ValidTo as not UTC")]
    public void Given_LocalValidTo_When_Validated_Then_NotUtc()
    {
        // Arrange
        var command = Valid();
        command.ValidTo = DateTime.SpecifyKind(Now.AddDays(10), DateTimeKind.Local);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == "ValidTo" && error.ErrorCode == "NotUtc");
    }

    [Theory(DisplayName = "Given an overlapping, unordered, or oversized tier set When validated Then reports InvalidTierSet on Tiers")]
    [InlineData(4, 10, 10, 20, 20)]
    [InlineData(10, 20, 4, 9, 20)]
    [InlineData(4, 9, 10, 25, 20)]
    public void Given_InvalidTierSet_When_Validated_Then_InvalidTierSet(int firstMin, int firstMax, int secondMin, int secondMax, int max)
    {
        // Arrange
        var command = Valid();
        command.MaxQuantityPerProduct = max;
        command.Tiers =
        [
            new DiscountTierInput { MinQuantity = firstMin, MaxQuantity = firstMax, Percentage = 10m },
            new DiscountTierInput { MinQuantity = secondMin, MaxQuantity = secondMax, Percentage = 20m }
        ];

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Tiers" && error.ErrorCode == "InvalidTierSet");
    }

    [Fact(DisplayName = "Given a null tier When validated Then is invalid without throwing")]
    public void Given_NullTier_When_Validated_Then_IsInvalid()
    {
        // Arrange
        var command = Valid();
        command.Tiers.Add(null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    private static CreateDiscountPolicyCommand Valid() => new()
    {
        ProductId = Guid.NewGuid(),
        BranchId = Guid.NewGuid(),
        ValidFrom = Now.AddDays(1),
        ValidTo = Now.AddDays(30),
        MaxQuantityPerProduct = 20,
        Tiers =
        [
            new DiscountTierInput { MinQuantity = 4, MaxQuantity = 9, Percentage = 10m },
            new DiscountTierInput { MinQuantity = 10, MaxQuantity = 20, Percentage = 20m }
        ]
    };
}
