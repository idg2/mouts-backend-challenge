using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Contains unit tests for the <see cref="CpfValidator"/> class. Values are already normalized (digits only).
/// </summary>
public class CpfValidatorTests
{
    private readonly CpfValidator _validator = new();

    /// <summary>
    /// Tests that CPFs with correct check digits pass.
    /// </summary>
    [Theory(DisplayName = "Given a CPF with correct check digits When validated Then is valid")]
    [InlineData("52998224725")]
    [InlineData("11144477735")]
    [InlineData("12345678909")]
    [InlineData("00000000191")]
    public void Given_CpfWithCorrectCheckDigits_When_Validated_Then_IsValid(string cpf)
    {
        // Act
        var result = _validator.Validate(cpf);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that wrong check digits, repeated digits, letters, and wrong lengths are rejected.
    /// </summary>
    [Theory(DisplayName = "Given an invalid CPF When validated Then is invalid")]
    [InlineData("52998224724")]
    [InlineData("52998224735")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("5299822472A")]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    public void Given_InvalidCpf_When_Validated_Then_IsInvalid(string cpf)
    {
        // Act
        var result = _validator.Validate(cpf);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
