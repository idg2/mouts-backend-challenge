using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for CreateCustomerRequest.
/// </summary>
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Initializes validation rules for CreateCustomerRequest: the name is required and has at most 100 characters,
    /// and the document, once its mask is removed, is a valid CPF or CNPJ.
    /// </summary>
    public CreateCustomerRequestValidator()
    {
        RuleFor(customer => customer.Name).NotEmpty().MaximumLength(100);

        RuleFor(customer => DocumentNumber.Normalize(customer.Document))
            .SetValidator(new DocumentValidator())
            .OverridePropertyName(nameof(CreateCustomerRequest.Document));
    }
}
