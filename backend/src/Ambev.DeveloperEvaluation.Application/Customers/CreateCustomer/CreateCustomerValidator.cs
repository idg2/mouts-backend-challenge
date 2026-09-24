using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for CreateCustomerCommand.
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Initializes validation rules for CreateCustomerCommand: the name is required and has at most 100 characters,
    /// and the document, once its mask is removed, is a valid CPF or CNPJ.
    /// </summary>
    public CreateCustomerValidator()
    {
        RuleFor(customer => customer.Name).NotEmpty().MaximumLength(100);

        RuleFor(customer => DocumentNumber.Normalize(customer.Document))
            .SetValidator(new DocumentValidator())
            .OverridePropertyName(nameof(CreateCustomerCommand.Document));
    }
}
