using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-015 (FEAT-010)
/// <summary>
/// Validator for the SaleItem entity.
/// </summary>
public class SaleItemValidator : AbstractValidator<SaleItem>
{
    /// <summary>
    /// Initializes the validation rules for SaleItem.
    /// </summary>
    /// <remarks>
    /// Rules: product id required; product description required, at most 200 characters; unit price
    /// greater than zero; quantity greater than zero; discount percentage between 0 and 100; discount
    /// amount and total amount not negative.
    /// </remarks>
    public SaleItemValidator()
    {
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductDescription).NotEmpty().MaximumLength(200);
        RuleFor(item => item.UnitPrice).GreaterThan(0m);
        RuleFor(item => item.Quantity).GreaterThan(0);
        RuleFor(item => item.DiscountPercentage).InclusiveBetween(0m, 100m);
        RuleFor(item => item.DiscountAmount).GreaterThanOrEqualTo(0m);
        RuleFor(item => item.TotalAmount).GreaterThanOrEqualTo(0m);
    }
}
