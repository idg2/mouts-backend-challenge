using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for CreateCustomerCommand.
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    /// <summary>
    /// Initializes validation rules for CreateCustomerCommand: the name is required and has at most 100 characters.
    /// </summary>
    public CreateCustomerValidator()
    {
        RuleFor(customer => customer.Name).NotEmpty().MaximumLength(100);
    }
}
