using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Validator for the Product entity.
/// </summary>
public class ProductValidator : AbstractValidator<Product>
{
    // Work item: BUG-009 (FEAT-010), FEAT-013
    /// <summary>
    /// Initializes the validation rules for Product: the code is required and has at most 50 characters, the description is required and has at most
    /// 200 characters, and the unit price is greater than zero and fits numeric(18,2).
    /// </summary>
    public ProductValidator()
    {
        RuleFor(product => product.Code)
            .NotEmpty()
            .MaximumLength(50).WithMessage("Product code cannot be longer than 50 characters.");

        RuleFor(product => product.Description)
            .NotEmpty()
            .MaximumLength(200).WithMessage("Product description cannot be longer than 200 characters.");

        RuleFor(product => product.UnitPrice)
            .GreaterThan(0m).WithMessage("Product unit price must be greater than zero.")
            .PrecisionScale(18, 2, true);
    }
}
