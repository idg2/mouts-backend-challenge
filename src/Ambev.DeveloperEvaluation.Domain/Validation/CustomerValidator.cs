using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Validator for the Customer entity.
/// </summary>
public class CustomerValidator : AbstractValidator<Customer>
{
    // Work item: TASK-014 (FEAT-010), FEAT-012
    /// <summary>
    /// Initializes the validation rules for Customer: the name is required and has at most 100 characters, and the
    /// document is a valid CPF or CNPJ.
    /// </summary>
    public CustomerValidator()
    {
        RuleFor(customer => customer.Name)
            .NotEmpty()
            .MaximumLength(100).WithMessage("Customer name cannot be longer than 100 characters.");

        // SetValidator skips a null property, so NotEmpty is repeated here to reject a null document.
        RuleFor(customer => customer.Document).NotEmpty().SetValidator(new DocumentValidator());
    }
}
