using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Validator for ListSalesCommand.
/// </summary>
public class ListSalesValidator : AbstractValidator<ListSalesCommand>
{
    /// <summary>
    /// Initializes validation rules for ListSalesCommand: page at least 1, size from 1 to 100.
    /// </summary>
    public ListSalesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
