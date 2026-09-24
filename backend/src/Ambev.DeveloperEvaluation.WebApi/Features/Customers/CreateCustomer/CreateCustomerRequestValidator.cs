using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for CreateCustomerRequest.
/// </summary>
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    /// <summary>
    /// Initializes validation rules for CreateCustomerRequest: the name is required and has at most 100 characters.
    /// </summary>
    public CreateCustomerRequestValidator()
    {
        RuleFor(customer => customer.Name).NotEmpty().MaximumLength(100);
    }
}
