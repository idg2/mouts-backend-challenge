using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Validates a normalized CNPJ, numeric or alphanumeric (issued since July 2026): 12 characters 0-9 or A-Z followed
/// by 2 check digits, not all the same. Each character counts as its ASCII code minus 48 in the modulo 11 check, so
/// digits keep their value and A is 17.
/// </summary>
public class CnpjValidator : AbstractValidator<string>
{
    private const int Length = 14;
    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Initializes the CNPJ rule.
    /// </summary>
    public CnpjValidator()
    {
        RuleFor(cnpj => cnpj)
            .Must(BeValidCnpj)
            .WithMessage("The CNPJ is not valid.");
    }

    private static bool BeValidCnpj(string cnpj)
    {
        if (cnpj.Length != Length
            || !cnpj[..12].All(character => char.IsAsciiDigit(character) || char.IsAsciiLetterUpper(character))
            || !cnpj[12..].All(char.IsAsciiDigit)
            || cnpj.Distinct().Count() == 1)
            return false;

        var values = cnpj.Select(character => character - '0').ToArray();
        return values[12] == CheckDigit(values, FirstWeights) && values[13] == CheckDigit(values, SecondWeights);
    }

    private static int CheckDigit(int[] values, int[] weights)
    {
        var sum = 0;
        for (var index = 0; index < weights.Length; index++)
            sum += values[index] * weights[index];

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
