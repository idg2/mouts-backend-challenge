using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

// Work item: TASK-022 (FEAT-010)
/// <summary>
/// Validator for UpdateSaleRequest.
/// </summary>
public class UpdateSaleRequestValidator : AbstractValidator<UpdateSaleRequest>
{
    /// <summary>
    /// Initializes validation rules for UpdateSaleRequest.
    /// </summary>
    /// <remarks>
    /// Rules: sale, customer, and branch ids required; total not negative; at least one item; item ids unique;
    /// per item, id not empty when present, product id required, quantity greater than zero, discount
    /// percentage between 0 and 100, discount amount and total not negative.
    /// </remarks>
    public UpdateSaleRequestValidator()
    {
        RuleFor(sale => sale.Id).NotEmpty().WithMessage("Sale ID is required");
        RuleFor(sale => sale.CustomerId).NotEmpty();
        RuleFor(sale => sale.BranchId).NotEmpty();
        RuleFor(sale => sale.TotalAmount).GreaterThanOrEqualTo(0m);
        RuleFor(sale => sale.Items).NotEmpty().WithMessage("A sale must have at least one item.");
        RuleFor(sale => sale.Items)
            .Must(items => items.Where(i => i.Id.HasValue).Select(i => i.Id).Distinct().Count()
                           == items.Count(i => i.Id.HasValue))
            .WithMessage("Item ids must be unique.");
        RuleForEach(sale => sale.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEqual(Guid.Empty).When(i => i.Id.HasValue);
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.DiscountPercentage).InclusiveBetween(0m, 100m);
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0m);
            item.RuleFor(i => i.TotalAmount).GreaterThanOrEqualTo(0m);
        });
    }
}
