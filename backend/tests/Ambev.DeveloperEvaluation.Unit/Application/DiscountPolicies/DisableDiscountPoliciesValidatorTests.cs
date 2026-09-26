using Ambev.DeveloperEvaluation.Application.DiscountPolicies.DisableDiscountPolicies;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.DiscountPolicies;

// Work item: TD-032
/// <summary>
/// Contains unit tests for the <see cref="DisableDiscountPoliciesValidator"/> class.
/// </summary>
public class DisableDiscountPoliciesValidatorTests
{
    private readonly DisableDiscountPoliciesValidator _validator = new();

    [Fact(DisplayName = "Given distinct non-empty ids When validated Then is valid")]
    public void Given_DistinctIds_When_Validated_Then_IsValid()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesCommand { Ids = [Guid.NewGuid(), Guid.NewGuid()] });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Given no ids When validated Then reports IdsRequired on Ids")]
    public void Given_NoIds_When_Validated_Then_IdsRequired()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesCommand { Ids = [] });

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be("IdsRequired");
    }

    [Fact(DisplayName = "Given a null id list When validated Then reports IdsRequired without throwing")]
    public void Given_NullIds_When_Validated_Then_IdsRequired()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesCommand { Ids = null! });

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be("IdsRequired");
    }

    [Fact(DisplayName = "Given an empty id When validated Then reports EmptyId on that entry")]
    public void Given_EmptyId_When_Validated_Then_EmptyId()
    {
        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesCommand { Ids = [Guid.NewGuid(), Guid.Empty] });

        // Assert
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Ids[1]").Which.ErrorCode.Should().Be("EmptyId");
    }

    [Fact(DisplayName = "Given a repeated id When validated Then reports DuplicateId naming it")]
    public void Given_RepeatedId_When_Validated_Then_DuplicateId()
    {
        // Arrange
        var repeated = Guid.NewGuid();

        // Act
        var result = _validator.Validate(new DisableDiscountPoliciesCommand { Ids = [repeated, Guid.NewGuid(), repeated] });

        // Assert
        var error = result.Errors.Should().ContainSingle().Which;
        error.ErrorCode.Should().Be("DuplicateId");
        error.ErrorMessage.Should().Contain(repeated.ToString());
    }
}
