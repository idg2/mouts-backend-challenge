using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Contains unit tests for the <see cref="CnpjValidator"/> class, numeric and alphanumeric. Values are already
/// normalized (no mask, upper case).
/// </summary>
public class CnpjValidatorTests
{
    private readonly CnpjValidator _validator = new();

    /// <summary>
    /// Tests that numeric and alphanumeric CNPJs with correct check digits pass; 12ABC34501DE35 is the Receita Federal example.
    /// </summary>
    [Theory(DisplayName = "Given a CNPJ with correct check digits When validated Then is valid")]
    [InlineData("11222333000181")]
    [InlineData("12ABC34501DE35")]
    [InlineData("60701190000104")]
    [InlineData("33000167000101")]
    public void Given_CnpjWithCorrectCheckDigits_When_Validated_Then_IsValid(string cnpj)
    {
        // Act
        var result = _validator.Validate(cnpj);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that wrong check digits, repeated characters, letters in the check digits, lower case, other
    /// characters, and wrong lengths are rejected.
    /// </summary>
    [Theory(DisplayName = "Given an invalid CNPJ When validated Then is invalid")]
    [InlineData("11222333000180")]
    [InlineData("12ABC34501DE36")]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("12ABC34501DEA5")]
    [InlineData("12abc34501de35")]
    [InlineData("12ABC34501D#35")]
    [InlineData("1222333000181")]
    [InlineData("112223330001810")]
    public void Given_InvalidCnpj_When_Validated_Then_IsInvalid(string cnpj)
    {
        // Act
        var result = _validator.Validate(cnpj);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
