using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

// Work item: TASK-021 (FEAT-010)
/// <summary>
/// Validator for CreateSaleRequest.
/// </summary>
public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    // Work item: BUG-007 (FEAT-010), BUG-009 (FEAT-010)
    /// <summary>
    /// Initializes validation rules for CreateSaleRequest.
    /// </summary>
    /// <remarks>
    /// Rules: customer and branch ids required; total not negative; at least one item; no null item; per item,
    /// product id required, quantity greater than zero, discount percentage between 0 and 100, discount amount
    /// and total not negative. Amounts fit numeric(18,2) and the percentage fits numeric(5,2).
    /// </remarks>
    public CreateSaleRequestValidator()
    {
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.TotalAmount).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
        RuleFor(sale => sale.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleForEach(sale => sale.Items).NotNull();
        RuleForEach(sale => sale.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.DiscountPercentage).InclusiveBetween(0m, 100m).PrecisionScale(5, 2, true);
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
            item.RuleFor(i => i.TotalAmount).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
        });
    }
}
