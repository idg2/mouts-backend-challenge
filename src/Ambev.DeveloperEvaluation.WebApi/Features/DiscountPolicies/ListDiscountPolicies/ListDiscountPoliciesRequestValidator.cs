using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.DiscountPolicies.ListDiscountPolicies;

// Work item: TASK-063 (FEAT-001)
/// <summary>
/// Validator for ListDiscountPoliciesRequest.
/// </summary>
public class ListDiscountPoliciesRequestValidator : AbstractValidator<ListDiscountPoliciesRequest>
{
    /// <summary>
    /// Initializes validation rules for ListDiscountPoliciesRequest: page at least 1, size from 1 to 100.
    /// </summary>
    public ListDiscountPoliciesRequestValidator()
    {
        RuleFor(request => request.Page).GreaterThanOrEqualTo(1);
        RuleFor(request => request.Size).InclusiveBetween(1, 100);
    }
}
