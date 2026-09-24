using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for UpdateCustomerCommand.
/// </summary>
public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    /// <summary>
    /// Initializes validation rules for UpdateCustomerCommand: the id is required; the name is required
    /// and has at most 100 characters.
    /// </summary>
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
