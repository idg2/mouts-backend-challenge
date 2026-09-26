using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.CreateDiscountPolicy;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.DiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Contains unit tests for the <see cref="CreateDiscountPolicyRequestValidator"/> class.
/// </summary>
public class CreateDiscountPolicyRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
    private readonly CreateDiscountPolicyRequestValidator _validator = new();

    [Fact(DisplayName = "Given a valid request When validated Then is valid")]
    public void Given_ValidRequest_When_Validated_Then_IsValid()
    {
        // Act
        var result = _validator.Validate(Valid());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Given a ValidFrom without Z When validated Then reports NotUtc on ValidFrom")]
    public void Given_UnspecifiedValidFrom_When_Validated_Then_NotUtc()
    {
        // Arrange
        var request = Valid();
        request.ValidFrom = DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Unspecified);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Should().Contain(error => error.PropertyName == "ValidFrom" && error.ErrorCode == "NotUtc");
    }

    // Work item: TASK-066 (FEAT-001)
    [Fact(DisplayName = "Given a ValidTo equal to ValidFrom When validated Then reports ValidToNotAfterValidFrom on ValidTo")]
    public void Given_ValidToEqualToValidFrom_When_Validated_Then_ValidToNotAfterValidFrom()
    {
        // Arrange
        var request = Valid();
        request.ValidTo = request.ValidFrom;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle(error => error.PropertyName == "ValidTo")
            .Which.ErrorCode.Should().Be("ValidToNotAfterValidFrom");
    }

    [Fact(DisplayName = "Given overlapping tiers When validated Then reports InvalidTierSet on Tiers")]
    public void Given_OverlappingTiers_When_Validated_Then_InvalidTierSet()
    {
        // Arrange
        var request = Valid();
        request.Tiers[0].MaxQuantity = 10;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Tiers" && error.ErrorCode == "InvalidTierSet");
    }

    [Fact(DisplayName = "Given null tiers When validated Then is invalid without throwing")]
    public void Given_NullTiers_When_Validated_Then_IsInvalid()
    {
        // Arrange
        var request = Valid();
        request.Tiers = null!;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.Select(error => error.PropertyName).Should().Contain("Tiers");
    }

    private static CreateDiscountPolicyRequest Valid() => new()
    {
        ProductId = Guid.NewGuid(),
        ValidFrom = Now.AddDays(1),
        MaxQuantityPerProduct = 20,
        Tiers =
        [
            new DiscountTierRequest { MinQuantity = 4, MaxQuantity = 9, Percentage = 10m },
            new DiscountTierRequest { MinQuantity = 10, MaxQuantity = 20, Percentage = 20m }
        ]
    };
}
