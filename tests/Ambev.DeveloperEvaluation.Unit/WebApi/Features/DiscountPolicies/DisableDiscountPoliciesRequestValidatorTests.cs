using Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.DisableDiscountPolicies;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Features.DiscountPolicies;

// Work item: TD-032
/// <summary>
/// Contains unit tests for the <see cref="DisableDiscountPoliciesRequestValidator"/> class.
/// </summary>
public class DisableDiscountPoliciesRequestValidatorTests
{
    private readonly DisableDiscountPoliciesRequestValidator _validator = new();

    [Fact(DisplayName = "Given distinct non-empty ids When validated Then is valid")]
    public void Given_DistinctIds_When_Validated_Then_IsValid()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesRequest { Ids = [Guid.NewGuid()] });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Given no ids When validated Then reports IdsRequired")]
    public void Given_NoIds_When_Validated_Then_IdsRequired()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesRequest { Ids = [] });

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be("IdsRequired");
    }

    [Fact(DisplayName = "Given an empty id When validated Then reports EmptyId on that entry")]
    public void Given_EmptyId_When_Validated_Then_EmptyId()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesRequest { Ids = [Guid.Empty] });

        // Assert
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Ids[0]").Which.ErrorCode.Should().Be("EmptyId");
    }

    [Fact(DisplayName = "Given a repeated id When validated Then reports DuplicateId naming it")]
    public void Given_RepeatedId_When_Validated_Then_DuplicateId()
    {
        // Arrange
        var repeated = Guid.NewGuid();

        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesRequest { Ids = [repeated, repeated] });

        // Assert
        var error = result.Errors.Should().ContainSingle().Which;
        error.ErrorCode.Should().Be("DuplicateId");
        error.ErrorMessage.Should().Contain(repeated.ToString());
    }
}
