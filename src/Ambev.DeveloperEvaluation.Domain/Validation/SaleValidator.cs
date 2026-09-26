using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-015 (FEAT-010)
/// <summary>
/// Validator for the Sale entity.
/// </summary>
public class SaleValidator : AbstractValidator<Sale>
{
    // Work item: BUG-009 (FEAT-010)
    /// <summary>
    /// Initializes the validation rules for Sale.
    /// </summary>
    /// <remarks>
    /// Rules: customer and branch ids required; customer and branch names required, at most 100
    /// characters; total amount not negative and fits numeric(18,2); at least one item; every item valid per
    /// <see cref="SaleItemValidator"/>.
    /// </remarks>
    public SaleValidator()
    {
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.BranchName).NotEmpty().MaximumLength(100);
        RuleFor(sale => sale.TotalAmount).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
        RuleFor(sale => sale.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleForEach(sale => sale.Items).SetValidator(new SaleItemValidator());
    }
}
