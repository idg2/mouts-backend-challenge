using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.DeleteCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for DeleteCustomerRequest.
/// </summary>
public class DeleteCustomerRequestValidator : AbstractValidator<DeleteCustomerRequest>
{
    /// <summary>
    /// Initializes validation rules for DeleteCustomerRequest.
    /// </summary>
    public DeleteCustomerRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Customer ID is required");
    }
}
