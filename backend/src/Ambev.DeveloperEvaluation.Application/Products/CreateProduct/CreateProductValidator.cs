using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

// Work item: TASK-020 (FEAT-010)
/// <summary>
/// Validator for CreateProductCommand.
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Initializes validation rules for CreateProductCommand: the description is required and has at most
    /// 200 characters; the unit price is greater than zero.
    /// </summary>
    public CreateProductValidator()
    {
        RuleFor(product => product.Description).NotEmpty().MaximumLength(200);
        RuleFor(product => product.UnitPrice).GreaterThan(0m);
    }
}
