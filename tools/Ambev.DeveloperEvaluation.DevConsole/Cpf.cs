namespace Ambev.DeveloperEvaluation.DevConsole;

// Work item: TASK-040 (FEAT-006), TASK-044 (FEAT-017)
/// <summary>
/// Generates valid CPF numbers, so each run registers its own customer (customer documents are unique).
/// </summary>
public static class Cpf
{
    /// <summary>
    /// Returns eleven digits: nine random ones, not all equal, followed by their two check digits.
    /// </summary>
    /// <param name="random">The random source</param>
    /// <returns>A valid CPF, digits only</returns>
    public static string Generate(Random random)
    {
        var digits = new int[11];
        do
        {
            for (var index = 0; index < 9; index++)
                digits[index] = random.Next(10);
        }
        while (digits.Take(9).Distinct().Count() == 1);

        digits[9] = CheckDigit(digits, 9);
        digits[10] = CheckDigit(digits, 10);
        return string.Concat(digits);
    }

    private static int CheckDigit(int[] digits, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
            sum += digits[index] * (length + 1 - index);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
