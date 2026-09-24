using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Contains unit tests for the <see cref="DocumentValidator"/> class and the <see cref="DocumentNumber"/> normalization.
/// </summary>
public class DocumentValidatorTests
{
    private readonly DocumentValidator _validator = new();

    /// <summary>
    /// Tests that a valid CPF or CNPJ passes, picked by its length.
    /// </summary>
    [Theory(DisplayName = "Given a valid CPF or CNPJ When validated Then is valid")]
    [InlineData("52998224725")]
    [InlineData("11222333000181")]
    [InlineData("12ABC34501DE35")]
    public void Given_ValidCpfOrCnpj_When_Validated_Then_IsValid(string document)
    {
        // Act
        var result = _validator.Validate(document);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that an empty document, a length that is neither CPF nor CNPJ, and a failing CPF or CNPJ are rejected.
    /// </summary>
    [Theory(DisplayName = "Given an invalid document When validated Then is invalid")]
    [InlineData("")]
    [InlineData("123456789012")]
    [InlineData("52998224724")]
    [InlineData("11222333000180")]
    public void Given_InvalidDocument_When_Validated_Then_IsInvalid(string document)
    {
        // Act
        var result = _validator.Validate(document);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a length that is neither CPF nor CNPJ is reported with the accepted lengths.
    /// </summary>
    [Fact(DisplayName = "Given a document of the wrong length When validated Then says which lengths are accepted")]
    public void Given_DocumentOfWrongLength_When_Validated_Then_SaysWhichLengthsAreAccepted()
    {
        // Act
        var result = _validator.Validate("123456789012");

        // Assert
        result.Errors.Select(error => error.ErrorMessage).Should()
            .Equal("The document must have 11 characters (CPF) or 14 characters (CNPJ).");
    }

    /// <summary>
    /// Tests that masks and surrounding spaces are removed and letters are put in upper case.
    /// </summary>
    [Theory(DisplayName = "Given a masked document When normalizing Then keeps only its characters in upper case")]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData(" 11.222.333/0001-81 ", "11222333000181")]
    [InlineData("12.abc.345/01de-35", "12ABC34501DE35")]
    [InlineData("12ABC34501DE35", "12ABC34501DE35")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Given_MaskedDocument_When_Normalizing_Then_KeepsOnlyItsCharactersInUpperCase(string? document, string expected)
    {
        // Act
        var normalized = DocumentNumber.Normalize(document);

        // Assert
        normalized.Should().Be(expected);
    }

    /// <summary>
    /// Tests that characters outside the mask are kept, so validation rejects them.
    /// </summary>
    [Fact(DisplayName = "Given a document with other characters When normalizing and validating Then is invalid")]
    public void Given_DocumentWithOtherCharacters_When_NormalizingAndValidating_Then_IsInvalid()
    {
        // Act
        var normalized = DocumentNumber.Normalize("529_982_247#25");

        // Assert
        normalized.Should().Be("529_982_247#25");
        _validator.Validate(normalized).IsValid.Should().BeFalse();
    }
}
