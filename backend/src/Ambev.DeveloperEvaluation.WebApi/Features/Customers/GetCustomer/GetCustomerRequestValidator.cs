using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.GetCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for GetCustomerRequest.
/// </summary>
public class GetCustomerRequestValidator : AbstractValidator<GetCustomerRequest>
{
    /// <summary>
    /// Initializes validation rules for GetCustomerRequest.
    /// </summary>
    public GetCustomerRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Customer ID is required");
    }
}
