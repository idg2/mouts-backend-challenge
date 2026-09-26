using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Validator for CreateSaleCommand.
/// </summary>
public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    // Work item: BUG-007 (FEAT-010), BUG-009 (FEAT-010), TASK-064 (FEAT-001)
    /// <summary>
    /// Initializes validation rules for CreateSaleCommand.
    /// </summary>
    /// <remarks>
    /// Rules: customer and branch ids required; at least one item; no null item; per item, product id required,
    /// quantity greater than zero, requested discount percentage null or between 0 and 100 with at most two decimals
    /// (numeric(5,2)). The discount policies are checked by the handler (SaleDiscountRules).
    /// </remarks>
    public CreateSaleValidator()
    {
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleForEach(sale => sale.Items).NotNull();
        RuleForEach(sale => sale.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.DiscountPercentage).InclusiveBetween(0m, 100m).PrecisionScale(5, 2, true);
        });
    }
}
