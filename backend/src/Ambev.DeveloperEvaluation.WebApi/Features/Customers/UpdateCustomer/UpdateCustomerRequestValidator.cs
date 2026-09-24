using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for UpdateCustomerRequest.
/// </summary>
public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    /// <summary>
    /// Initializes validation rules for UpdateCustomerRequest: the id is required; the name is required
    /// and has at most 100 characters.
    /// </summary>
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
