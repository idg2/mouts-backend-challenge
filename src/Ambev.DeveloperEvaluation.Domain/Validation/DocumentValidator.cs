using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: FEAT-012
/// <summary>
/// Validates a normalized customer document: a CPF when it has 11 characters, a CNPJ when it has 14.
/// </summary>
public class DocumentValidator : AbstractValidator<string>
{
    /// <summary>
    /// Initializes the document rules.
    /// </summary>
    public DocumentValidator()
    {
        RuleFor(document => document)
            .NotEmpty()
            .WithMessage("The document cannot be empty.");

        RuleFor(document => document)
            .Must(document => document.Length is 11 or 14)
            .WithMessage("The document must have 11 characters (CPF) or 14 characters (CNPJ).")
            .When(document => !string.IsNullOrEmpty(document));

        RuleFor(document => document)
            .SetValidator(new CpfValidator())
            .When(document => document.Length == 11);

        RuleFor(document => document)
            .SetValidator(new CnpjValidator())
            .When(document => document.Length == 14);
    }
}
