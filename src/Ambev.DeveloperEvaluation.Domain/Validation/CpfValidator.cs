using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Validates a normalized CPF: 11 digits, not all the same, with both check digits correct (modulo 11).
/// </summary>
public class CpfValidator : AbstractValidator<string>
{
    private const int Length = 11;

    /// <summary>
    /// Initializes the CPF rule.
    /// </summary>
    public CpfValidator()
    {
        RuleFor(cpf => cpf)
            .Must(BeValidCpf)
            .WithMessage("The CPF is not valid.");
    }

    private static bool BeValidCpf(string cpf)
    {
        if (cpf.Length != Length || !cpf.All(char.IsAsciiDigit) || cpf.Distinct().Count() == 1)
            return false;

        var digits = cpf.Select(character => character - '0').ToArray();
        return digits[9] == CheckDigit(digits, 9) && digits[10] == CheckDigit(digits, 10);
    }

    // Weights run from count + 1 down to 2 over the first count digits.
    private static int CheckDigit(int[] digits, int count)
    {
        var sum = 0;
        for (var index = 0; index < count; index++)
            sum += digits[index] * (count + 1 - index);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
