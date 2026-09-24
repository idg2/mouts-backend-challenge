using Ambev.DeveloperEvaluation.Domain.Validation;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for UpdateCustomerRequest.
/// </summary>
public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    // Work item: TASK-018 (FEAT-010), FEAT-012
    /// <summary>
    /// Initializes validation rules for UpdateCustomerRequest: the id is required; the name is required
    /// and has at most 100 characters; the document, once its mask is removed, is a valid CPF or CNPJ.
    /// </summary>
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => DocumentNumber.Normalize(x.Document))
            .SetValidator(new DocumentValidator())
            .OverridePropertyName(nameof(UpdateCustomerRequest.Document));
    }
}
