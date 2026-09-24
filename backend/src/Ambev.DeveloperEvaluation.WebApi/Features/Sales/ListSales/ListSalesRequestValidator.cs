using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Validator for ListSalesRequest.
/// </summary>
public class ListSalesRequestValidator : AbstractValidator<ListSalesRequest>
{
    /// <summary>
    /// Initializes validation rules for ListSalesRequest: page at least 1, size from 1 to 100.
    /// </summary>
    public ListSalesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
