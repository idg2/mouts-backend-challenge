using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-015 (FEAT-010)
/// <summary>
/// Validator for the SaleItem entity.
/// </summary>
public class SaleItemValidator : AbstractValidator<SaleItem>
{
    // Work item: BUG-009 (FEAT-010), TD-010 (FEAT-010), TASK-062 (FEAT-001)
    /// <summary>
    /// Initializes the validation rules for SaleItem.
    /// </summary>
    /// <remarks>
    /// Rules: line number greater than zero; product id required; product description required, at most 200 characters; unit price
    /// greater than zero; quantity greater than zero; requested discount null or between 0 and 100; discount policy id
    /// required; discount ceiling between 0 and 100; applied discount between 0 and 100 and not above the ceiling;
    /// discount amount and total amount not negative. Amounts fit numeric(18,2) and percentages fit numeric(5,2).
    /// </remarks>
    public SaleItemValidator()
    {
        RuleFor(item => item.LineNumber).GreaterThan(0);
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductDescription).NotEmpty().MaximumLength(200);
        RuleFor(item => item.UnitPrice).GreaterThan(0m).PrecisionScale(18, 2, true);
        RuleFor(item => item.Quantity).GreaterThan(0);
        RuleFor(item => item.RequestedDiscountPercentage).InclusiveBetween(0m, 100m).PrecisionScale(5, 2, true);
        RuleFor(item => item.DiscountPolicyId).NotEmpty();
        RuleFor(item => item.DiscountCeilingPercentage).InclusiveBetween(0m, 100m).PrecisionScale(5, 2, true);
        RuleFor(item => item.DiscountPercentage).InclusiveBetween(0m, 100m).PrecisionScale(5, 2, true)
            .LessThanOrEqualTo(item => item.DiscountCeilingPercentage);
        RuleFor(item => item.DiscountAmount).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
        RuleFor(item => item.TotalAmount).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
    }
}
