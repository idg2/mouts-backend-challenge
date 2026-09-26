using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Validator for ListDiscountPoliciesCommand.
/// </summary>
public class ListDiscountPoliciesValidator : AbstractValidator<ListDiscountPoliciesCommand>
{
    /// <summary>
    /// Initializes validation rules for ListDiscountPoliciesCommand: page at least 1, size from 1 to 100.
    /// </summary>
    public ListDiscountPoliciesValidator()
    {
        RuleFor(command => command.Page).GreaterThanOrEqualTo(1);
        RuleFor(command => command.Size).InclusiveBetween(1, 100);
    }
}
