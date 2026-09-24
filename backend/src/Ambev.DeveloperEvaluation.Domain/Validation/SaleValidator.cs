using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-015 (FEAT-010)
/// <summary>
/// Validator for the Sale entity.
/// </summary>
public class SaleValidator : AbstractValidator<Sale>
{
    /// <summary>
    /// Initializes the validation rules for Sale.
    /// </summary>
    /// <remarks>
    /// Rules: customer and branch ids required; customer and branch names required, at most 100
    /// characters; total amount not negative; at least one item; every item valid per
    /// <see cref="SaleItemValidator"/>.
    /// </remarks>
    public SaleValidator()
    {
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.BranchName).NotEmpty().MaximumLength(100);
        RuleFor(sale => sale.TotalAmount).GreaterThanOrEqualTo(0m);
        RuleFor(sale => sale.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleForEach(sale => sale.Items).SetValidator(new SaleItemValidator());
    }
}
