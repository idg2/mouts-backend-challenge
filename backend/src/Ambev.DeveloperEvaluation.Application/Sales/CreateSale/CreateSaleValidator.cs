using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Validator for CreateSaleCommand.
/// </summary>
public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>
    /// Initializes validation rules for CreateSaleCommand.
    /// </summary>
    /// <remarks>
    /// Rules: customer and branch ids required; total not negative; at least one item; per item, product id
    /// required, quantity greater than zero, discount percentage between 0 and 100, discount amount and total
    /// not negative. README discount rules are not enforced here (FEAT-001).
    /// </remarks>
    public CreateSaleValidator()
    {
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.TotalAmount).GreaterThanOrEqualTo(0m);
        RuleFor(sale => sale.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleForEach(sale => sale.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.DiscountPercentage).InclusiveBetween(0m, 100m);
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0m);
            item.RuleFor(i => i.TotalAmount).GreaterThanOrEqualTo(0m);
        });
    }
}
