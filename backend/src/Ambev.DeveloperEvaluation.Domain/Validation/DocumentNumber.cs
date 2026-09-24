namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Normalizes a CPF or CNPJ to the form it is validated and stored in.
/// </summary>
public static class DocumentNumber
{
    /// <summary>
    /// Removes the mask characters ('.', '-', '/') and white space and puts letters in upper case. Any other character
    /// is kept, so validation rejects it.
    /// </summary>
    /// <param name="value">The document as the client sent it</param>
    /// <returns>The normalized document; empty when the value is null</returns>
    public static string Normalize(string? value) =>
        string.Concat((value ?? string.Empty).Where(character => character is not ('.' or '-' or '/') && !char.IsWhiteSpace(character)))
            .ToUpperInvariant();
}
