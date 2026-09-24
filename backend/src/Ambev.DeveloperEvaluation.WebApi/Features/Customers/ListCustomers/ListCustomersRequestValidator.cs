using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers.ListCustomers;

// Work item: TASK-018 (FEAT-010)
/// <summary>
/// Validator for ListCustomersRequest.
/// </summary>
public class ListCustomersRequestValidator : AbstractValidator<ListCustomersRequest>
{
    /// <summary>
    /// Initializes validation rules for ListCustomersRequest: page at least 1, size from 1 to 100.
    /// </summary>
    public ListCustomersRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
