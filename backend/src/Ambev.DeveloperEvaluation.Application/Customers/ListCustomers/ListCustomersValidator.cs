using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for ListCustomersCommand.
/// </summary>
public class ListCustomersValidator : AbstractValidator<ListCustomersCommand>
{
    /// <summary>
    /// Initializes validation rules for ListCustomersCommand: page at least 1, size from 1 to 100.
    /// </summary>
    public ListCustomersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
