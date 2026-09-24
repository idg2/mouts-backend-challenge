using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Domain.Validation;

// Work item: TASK-014 (FEAT-010)
/// <summary>
/// Validator for the Product entity.
/// </summary>
public class ProductValidator : AbstractValidator<Product>
{
    /// <summary>
    /// Initializes the validation rules for Product: the description is required and has at most
    /// 200 characters, and the unit price is greater than zero.
    /// </summary>
    public ProductValidator()
    {
        RuleFor(product => product.Description)
            .NotEmpty()
            .MaximumLength(200).WithMessage("Product description cannot be longer than 200 characters.");

        RuleFor(product => product.UnitPrice)
            .GreaterThan(0m).WithMessage("Product unit price must be greater than zero.");
    }
}
